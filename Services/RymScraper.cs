using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Cookie;
using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Models;

namespace RymRss.Services;

public class RymScraper : BackgroundService
{
    private static readonly CultureInfo RymCulture = CultureInfo.CreateSpecificCulture("en-US");
    private readonly IServiceProvider ServiceProvider;

    public RymScraper(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var document = await GetDocument(cancellationToken);
        var pageData = ExtractPageAlbumData(document);

        using var scope = ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RymRssContext>();
        await UpdateDbAlbumData(dbContext, pageData, cancellationToken);
    }

    private Task<IDocument> GetDocument(CancellationToken cancellationToken)
    {
        var cookies = new[]
        {
            "is_logged_in=1; Expires=Tue, 01-Jan-2030 01:00:00 GMT; Path=/; HttpOnly;",
            "username=jiux; Expires=Tue, 01-Jan-2030 01:00:00 GMT; Path=/; secure; HttpOnly;",
        };

        var cookiesFileHandler = new LocalFileHandler("cookies.txt");
        var cookieProvider = new AdvancedCookieProvider(cookiesFileHandler);
        foreach (var cookie in cookies)
        {
            cookieProvider.AddCookie(WebCookie.FromString(cookie + "; Domain=rateyourmusic.com"));
        }

        var config = Configuration.Default
            .WithCookies(cookieProvider)
            .WithDefaultLoader();
        var address = "https://rateyourmusic.com/~jiux";
        var context = BrowsingContext.New(config);

        return context.OpenAsync(address, cancellationToken);
    }

    private IEnumerable<AlbumData> ExtractPageAlbumData(IDocument document)
    {
        var artists = document.QuerySelectorAll(".artist").Cast<IHtmlAnchorElement>();
        var albums = document.QuerySelectorAll(".album").Cast<IHtmlAnchorElement>();
        var parentContainer = artists.First().ParentElement;
        var dates = parentContainer.QuerySelectorAll("div > b");

        return albums.Zip(artists, (album, artist) =>
            {
                var date = dates.Last(date => date.CompareDocumentPosition(album).HasFlag(DocumentPositions.Following));
                return new { album, artist, date };
            })
            .Select(pageElements => new AlbumData
            {
                Title = pageElements.album.TextContent,
                AlbumId = pageElements.album.Title ?? "",
                AlbumHref = pageElements.album.Href,
                Artist = pageElements.artist.TextContent,
                ArtistId = pageElements.artist.Title ?? "",
                ArtistHref = pageElements.artist.Href,
                ReleaseDate = DateOnly.Parse(pageElements.date.TextContent, RymCulture),
            });
    }

    private async Task UpdateDbAlbumData(RymRssContext dbContext, IEnumerable<AlbumData> pageAlbums, CancellationToken cancellationToken)
    {
        var pageAlbumsList = pageAlbums.ToList();

        var pageAlbumIds = pageAlbumsList.Select(albumData => albumData.AlbumId);
        var existingDbAlbums = await dbContext.Albums
            .Where(album => pageAlbumIds.Contains(album.AlbumId))
            .ToDictionaryAsync(
                album => album,
                album => pageAlbumsList.First(albumData => albumData.AlbumId == album.AlbumId),
                cancellationToken);
        foreach (var (dbAlbum, pageAlbumData) in existingDbAlbums)
        {
            if (dbAlbum.Equals(pageAlbumData)) continue;
            dbAlbum.Update(pageAlbumData);
        }

        var newAlbums = pageAlbumsList
            .Where(albumData => !existingDbAlbums.ContainsValue(albumData))
            .Select(albumData => new Album(albumData));
        dbContext.Albums.AddRange(newAlbums);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
