namespace RymRss.Models;

public class Album
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Html { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }
}
