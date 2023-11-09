namespace RymRss.Models;

public class Album : AlbumData
{
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

    public DateTime DateLastChanged
    {
        get
        {
            var dates = new List<DateTime>(3) { DateCreated };
            if (DateUpdated is {} dateUpdated) dates.Add(dateUpdated);
            if (IsReleased) dates.Add(ReleaseDate.ToDateTime(TimeOnly.MinValue));
            return dates.Max();
        }
    }
}

public class AlbumData : IEquatable<AlbumData>
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Href { get; set; }
    public DateOnly ReleaseDate { get; set; }

    public List<Artist> Artists { get; set; } = new();

    public bool Equals(AlbumData? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Title == other.Title && Href == other.Href && ReleaseDate.Equals(other.ReleaseDate)
            && Artists.All(artist => other.Artists.Exists(otherArtist => otherArtist.Equals(artist)))
            && other.Artists.All(otherArtist => Artists.Exists(artist => artist.Equals(otherArtist)));
    }

    public void CopyValues(AlbumData other)
    {
        Title = other.Title;
        Id = other.Id;
        Href = other.Href;
        Artists = other.Artists;
        ReleaseDate = other.ReleaseDate;
    }

    public bool IsReleased => DateOnly.FromDateTime(DateTime.UtcNow) >= ReleaseDate;
    public ReleaseType ReleaseType => Href.Contains(@"/ep/") ? ReleaseType.Ep : ReleaseType.Album;
}

public enum ReleaseType
{
    Album,
    Ep,
}
