using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using babylog.Models;
using Microsoft.Azure.Cosmos;
namespace babylog.Controllers;

public class HomeController : Controller
{
    private readonly Container container;
    private readonly ILogger<HomeController> logger;

    public HomeController(ILogger<HomeController> logger)
    {
        logger = logger;
        CosmosClient client = new(connectionString: Keys.ConnectionString);
        Database database = client.GetDatabase("pranaydey").ReadAsync().GetAwaiter().GetResult();
        container = database.GetContainer("pranaydey").ReadContainerAsync().GetAwaiter().GetResult();
    }

    private string GetKey()
    {
        TimeZoneInfo pacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        DateTime pacificTimeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pacificTimeZone);
        return pacificTimeZoneNow.ToShortDateString().Replace("/","-");
    }

    private TimeSpan GetTime()
    {
        TimeZoneInfo pacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        DateTime pacificTimeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pacificTimeZone);
        return pacificTimeZoneNow.TimeOfDay;
    }

    public bool TimeNullCurrentCompare(string? shouldBeNull, string? shouldNotBeNull, int withinHours)
    {
        if (shouldBeNull != null || shouldNotBeNull == null)
            return false;
        var time = GetTime();
        if (time.Subtract(TimeSpan.Parse(shouldNotBeNull)).Hours >= withinHours)
        {
            return true;
        }
        return false;
    }

    public void UpdateViewData(DayLog daylog)
    {
        ViewData["Date"] = GetKey();
        var time = GetTime();
        ViewData["Time"] = time.ToString(@"hh\:mm");
        if ((time >= TimeSpan.FromHours(12) && daylog.medicine < 5.0f) || (time >= TimeSpan.FromHours(15) && daylog.medicine < 10.0f))
        {
            ViewData["MedicineColor"] = "#f4c7d0";
        }
        else
        {
            ViewData["MedicineColor"] = "#9faa74";
        }
        ViewData["DiaperColor"] = "#d7dab3";
        if (daylog.diaperTimes[0] == null)
        {
            ViewData["DiaperColor"] = "#f4c7d0";
        }
        else
        {
            for (int i = 8; i >= 0; i--)
            {
                if (daylog.diaperTimes[i] != null)
                {
                    if (time.Subtract(TimeSpan.Parse(daylog.diaperTimes[i])).Hours >= 3)
                    {
                        ViewData["DiaperColor"] = "#f4c7d0";
                    }
                    break;
                }
            }
        }
        ViewData["FeedColor"] = "#9faa74";
        if (daylog.feedTimes[0] == null)
        {
            ViewData["FeedColor"] = "#c66f80";
        }
        else
        {
            for (int i = 8; i >= 0; i--)
            {
                if (daylog.feedTimes[i] != null)
                {
                    if (time.Subtract(TimeSpan.Parse(daylog.feedTimes[i])).Hours >= 2)
                    {
                        ViewData["FeedColor"] = "#c66f80";
                    }
                    break;
                }
            }
        }
        ViewData["NapColor"] = "#d7dab3";
        if (daylog.wakeUpTime == null)
        {
            ViewData["NapColor"] = "#f4c7d0";
        }
        else
        {
            for (int i = 2; i >= 0; i--)
            {
                if (daylog.napSleepTimes[i] != null && daylog.napWakeTimes[i] == null)
                {
                    if (time.Subtract(TimeSpan.Parse(daylog.napSleepTimes[i])).Hours >= 2)
                    {
                        ViewData["NapColor"] = "#f4c7d0";
                    }
                    break;
                }
                else if (daylog.napSleepTimes[i] == null && daylog.napWakeTimes[i] == null && (i == 0 || daylog.napWakeTimes[i - 1] != null))
                {
                    var wakeTime = i == 0 ? daylog.wakeUpTime : daylog.napWakeTimes[i - 1];
                    if (time.Subtract(TimeSpan.Parse(wakeTime)).Hours >= 3)
                    {
                        ViewData["NapColor"] = "#f4c7d0";
                    }
                    break;
                }                      
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> Index(DayLog daylog)
    {
        var key = GetKey();
        try
        {
            daylog = await container.UpsertItemAsync(daylog, new PartitionKey(key));
        }
        catch (Exception e)
        {
            ViewData["log"] = "post : " + daylog.id + "," + daylog.medicine + "," + daylog.wakeUpTime + " - " + e.Message;
        }
        UpdateViewData(daylog);
        return View(daylog);
    }

    public async Task<IActionResult> Index()
    {
        var key = GetKey();
        DayLog daylog = null;
        try{
            try
            {
                daylog = await container.ReadItemAsync<DayLog>(key, new PartitionKey(key));
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                daylog = new DayLog
                {
                    id = key,
                    medicine = 0,
                    diaperTimes = new string?[9],
                    diaperTypes = new string?[9],
                    feedTimes = new string?[9],
                    feedTypes = new string?[9],
                    feedQuantities = new float?[9],
                    napSleepTimes = new string?[3],
                    napWakeTimes = new string?[3],
                };
                daylog = await container.UpsertItemAsync(daylog, new PartitionKey(daylog.id));
            }
        } catch (Exception e)
        {
            ViewData["log"] = "post : " + daylog.id + "," + daylog.medicine + "," + daylog.wakeUpTime + " - " + e.Message;
        }
        UpdateViewData(daylog);
        return View(daylog);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
