namespace RymRss.Models;

public class Album : AlbumData
{
    public int Id { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }

    public Album() {}
    public Album(AlbumData data)
    {
        CopyValues(data);
        DateCreated = DateTime.UtcNow;
    }

    public void Update(AlbumData data)
    {
        CopyValues(data);
        DateUpdated = DateTime.UtcNow;
    }
}

public class AlbumData : IEquatable<AlbumData>
{
    public string Title { get; set; }
    public string AlbumId { get; set; }
    public string AlbumHref { get; set; }
    public string Artist { get; set; }
    public string ArtistId { get; set; }
    public string ArtistHref { get; set; }
    public DateOnly ReleaseDate { get; set; }

    public bool Equals(AlbumData? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Title == other.Title && AlbumId == other.AlbumId && AlbumHref == other.AlbumHref && Artist == other.Artist && ArtistId == other.ArtistId && ArtistHref == other.ArtistHref && ReleaseDate.Equals(other.ReleaseDate);
    }

    public void CopyValues(AlbumData other)
    {
        Title = other.Title;
        AlbumId = other.AlbumId;
        AlbumHref = other.AlbumHref;
        Artist = other.Artist;
        ArtistId = other.ArtistId;
        ArtistHref = other.ArtistHref;
        ReleaseDate = other.ReleaseDate;
    }
}
