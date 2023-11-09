namespace RymRss.Models;

public class Artist : ArtistData
{
    public List<Album> Albums { get; } = new();
}

public class ArtistData : IEquatable<ArtistData>
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Href { get; set; }

    public bool Equals(ArtistData? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Name == other.Name && Href == other.Href;
    }
}
