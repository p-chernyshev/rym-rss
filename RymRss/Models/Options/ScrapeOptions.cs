namespace RymRss.Models.Options;

public class ScrapeOptions
{
    public string User { get; set; }
    public double IntervalMinutes { get; set; } = 60;
    public bool CheckOnLaunch { get; set; } = true;
    public string[] Cookies { get; set; }
}
