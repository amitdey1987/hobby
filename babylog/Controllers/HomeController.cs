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

        CosmosClient client = new(connectionString: ConnectionString);
        Database database = client.GetDatabase("pranaydey").ReadAsync().GetAwaiter().GetResult();
        container = database.GetContainer("pranaydey").ReadContainerAsync().GetAwaiter().GetResult();
    }

    public IActionResult Index()
    {
        DayLog daylog = null;
        try
        {
            daylog = container.ReadItemAsync<DayLog>("1", new PartitionKey("1")).GetAwaiter().GetResult();
            daylog = new DayLog(id: daylog.id, medicine: daylog.medicine + 0.5f);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            daylog = new DayLog(id: daylog.id, medicine: 0.0f);
        }
        daylog = container.UpsertItemAsync(daylog, new PartitionKey(daylog.id)).GetAwaiter().GetResult();
        ViewData["medicine"] = daylog.medicine;
        return View();
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
