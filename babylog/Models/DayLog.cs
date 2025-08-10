namespace babylog.Models;

using System.ComponentModel.DataAnnotations;

public class DayLog
{
    public string id { get; set; }
    public float medicine { get; set; }

    public float medicine2 { get; set; }
    public string wakeUpTime { get; set; }
    public string?[] napSleepTimes { get; set; }
    public string?[] napWakeTimes { get; set; }
    public string?[] diaperTimes { get; set; }
    public string?[] diaperTypes { get; set; }
    public string?[] feedTimes { get; set; }
    public string?[] feedTypes { get; set; }
    public string?[] feedQuantities { get; set; }    
    public string notes { get; set; }
}