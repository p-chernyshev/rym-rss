namespace RymRss.Models;

public class Album : AlbumData
{
    public int Id { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }
}

public class AlbumData
{
    public string Title { get; set; }
    public string AlbumId { get; set; }
    public string AlbumHref { get; set; }
    public string Artist { get; set; }
    public string ArtistId { get; set; }
    public string ArtistHref { get; set; }
    public DateOnly ReleaseDate { get; set; }
}
