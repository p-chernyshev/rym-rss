using RymRss.Extensions;

namespace RymRss.Models;

public class AlbumData
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Href { get; set; }
    public DateOnly ReleaseDate { get; set; }

    protected void CopyValues(AlbumData other)
    {
        Id = other.Id;
        Title = other.Title;
        Href = other.Href;
        ReleaseDate = other.ReleaseDate;
    }

    public bool IsReleased => DateOnly.FromDateTime(DateTime.UtcNow) >= ReleaseDate;
    public ReleaseType ReleaseType => Href.Contains(@"/ep/") ? ReleaseType.Ep : ReleaseType.Album;
}

public class Album : AlbumData, IEquatable<PageAlbumData>
{
    public DateTime DateCreated { get; set; }
    public DateTime? DateUpdated { get; set; }

    public List<Artist> Artists { get; set; } = new();

    public Album() {}
    public Album(PageAlbumData data, IEnumerable<Artist> allArtists)
    {
        CopyValues(data);
        Artists = data.Artists
            .Select(artistData => allArtists.Single(artist => artist.Id == artistData.Id))
            .ToList();
        DateCreated = DateTime.UtcNow;
    }

    public void Update(PageAlbumData data, IEnumerable<Artist> allArtists)
    {
        CopyValues(data);
        Artists = data.Artists
            .Select(artistData => allArtists.Single(artist => artist.Id == artistData.Id))
            .ToList();
        DateUpdated = DateTime.UtcNow;
    }

    public bool Equals(PageAlbumData? other)
    {
        if (ReferenceEquals(null, other)) return false;
        return Id == other.Id && Title == other.Title && Href == other.Href && ReleaseDate.Equals(other.ReleaseDate)
               && Artists.EquivalentBy(other.Artists, artist => artist.Id);
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

public class PageAlbumData : AlbumData
{
    public List<ArtistData> Artists { get; set; } = new();
}

public enum ReleaseType
{
    Album,
    Ep,
}
