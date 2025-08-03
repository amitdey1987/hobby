namespace babylog.Models;

using System.ComponentModel.DataAnnotations;

public class DayLog
{
    public string id { get; set; }
    public float medicine { get; set; }
    public string wakeUpTime { get; set; }
    public string nap1SleepTime { get; set; }
    public string nap1WakeTime { get; set; }
    public string nap2SleepTime { get; set; }
    public string nap2WakeTime { get; set; }
    public string nap3SleepTime { get; set; }
    public string nap3WakeTime { get; set; }

    public string Diaper1Time { get; set; }

    public string Diaper1Type { get; set; }
}