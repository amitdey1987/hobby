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
            ViewData["MedicineColor"] = "#FF0000";
        }
        else
        {
            ViewData["MedicineColor"] = "#00FF00";
        }
        if (TimeNullCurrentCompare(daylog.nap3WakeTime, daylog.nap3SleepTime, 2)
            || TimeNullCurrentCompare(daylog.nap3SleepTime, daylog.nap2WakeTime, 3)
            || TimeNullCurrentCompare(daylog.nap2WakeTime, daylog.nap2SleepTime, 2)
            || TimeNullCurrentCompare(daylog.nap2SleepTime, daylog.nap1WakeTime, 3)
            || TimeNullCurrentCompare(daylog.nap1WakeTime, daylog.wakeUpTime, 3)
            || daylog.wakeUpTime == null)
        {
            ViewData["NapColor"] = "#FF0000";
        }
        else
        {
            ViewData["NapColor"] = "#00FF00";
        }
        ViewData["FeedColor"] = "#FF0000";
        ViewData["DiaperColor"] = "#00FF00";
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
                daylog = new DayLog { id = key, medicine = 0, diaperTimes = new string?[9] };
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
