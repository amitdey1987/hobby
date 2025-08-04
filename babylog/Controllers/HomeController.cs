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
        return pacificTimeZoneNow.ToShortDateString();
    }

    private string GetTime()
    {
        TimeZoneInfo pacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        DateTime pacificTimeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pacificTimeZone);
        return pacificTimeZoneNow.TimeOfDay.ToString(@"hh\:mm");
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
        ViewData["Date"] = key;
        ViewData["Time"] = GetTime();
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
                daylog = new DayLog { id = key, medicine = 0 };
                daylog = await container.UpsertItemAsync(daylog, new PartitionKey(daylog.id));
            }
        } catch (Exception e)
        {
            ViewData["log"] = "post : " + daylog.id + "," + daylog.medicine + "," + daylog.wakeUpTime + " - " + e.Message;
        }
        ViewData["Date"] = key;
        ViewData["Time"] = GetTime();
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
