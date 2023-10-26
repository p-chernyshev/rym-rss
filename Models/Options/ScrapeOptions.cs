namespace RymRss.Models.Options;

public class ScrapeOptions
{
    public string User { get; set; }
    public double IntervalMinutes { get; set; }
    public bool CheckOnLaunch { get; set; }
    public string[]? Cookies { get; set; }
}
