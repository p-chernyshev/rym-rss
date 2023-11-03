using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Cookie;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RymRss.Db;
using RymRss.Models;
using RymRss.Models.Options;

namespace RymRss.Services;

public class RymScraper : BackgroundService
{
    private static readonly CultureInfo RymCulture = CultureInfo.CreateSpecificCulture("en-US");
    private const int IntervalMinutesMultiplier = 60 * 1000;

    private readonly ILogger<RymScraper> Logger;
    private readonly IServiceProvider ServiceProvider;
    private readonly ScrapeOptions ScrapeOptions;
    private readonly AppOptions AppOptions;

    public RymScraper(
        IServiceProvider serviceProvider,
        ILogger<RymScraper> logger,
        IOptions<ScrapeOptions> scrapeOptions,
        IOptions<AppOptions> appOptions
    )
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        ScrapeOptions = scrapeOptions.Value;
        AppOptions = appOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (ScrapeOptions.CheckOnLaunch) await ScrapeRymPageAlbums(cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay((int)(ScrapeOptions.IntervalMinutes * IntervalMinutesMultiplier), cancellationToken);
            await ScrapeRymPageAlbums(cancellationToken);
        }
    }

    private async Task ScrapeRymPageAlbums(CancellationToken cancellationToken)
    {
        try
        {
            var document = await GetDocument(cancellationToken);
            var pageData = ExtractPageAlbumData(document).ToList();

            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RymRssContext>();
            await UpdateDbAlbumData(dbContext, pageData, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error updating albums");
        }
    }

    private async Task<IDocument> GetDocument(CancellationToken cancellationToken)
    {
        var cookiesFileHandler = new LocalFileHandler(Path.Join(AppOptions.DataFolder, "cookies.txt"));
        var cookieProvider = new AdvancedCookieProvider(cookiesFileHandler);
        if (ScrapeOptions.Cookies is { } cookies)
        {
            foreach (var cookie in cookies)
            {
                cookieProvider.AddCookie(WebCookie.FromString(cookie + "; Domain=rateyourmusic.com"));
            }
        }

        var config = AngleSharp.Configuration.Default
            .WithCookies(cookieProvider)
            .WithDefaultLoader();
        var context = BrowsingContext.New(config);
        var address = $"https://rateyourmusic.com/~{ScrapeOptions.User}";

        var document = await context.OpenAsync(address, cancellationToken);
        if ((int)document.StatusCode >= 400) throw new Exception($"Failed to load document '{address}', status: {(int)document.StatusCode} {document.StatusCode}");
        Logger.LogDebug("Document '{Address}' loaded with status {StatusCode} {StatusText}", address, (int)document.StatusCode, document.StatusCode);
        if (document.Body is null || string.IsNullOrWhiteSpace(document.Body.Html())) throw new Exception("Loaded document is empty");

        return document;
    }

    private IEnumerable<AlbumData> ExtractPageAlbumData(IDocument document)
    {
        var upcomingHeader = document.QuerySelectorAll("th").Single(element => element.Text().Contains("Upcoming"));
        var upcomingListBlock = upcomingHeader.ParentElement?.NextElementSibling?.FirstElementChild?.FirstElementChild;
        if (upcomingListBlock is null) throw new Exception("Couldn't find upcoming albums list on page");
        var artists = upcomingListBlock.QuerySelectorAll<IHtmlAnchorElement>(".artist").ToList();
        var albums = upcomingListBlock.QuerySelectorAll<IHtmlAnchorElement>(".album").ToList();
        if (artists.Count == 0 || albums.Count == 0)
        {
            Logger.LogInformation("Parsed list of albums is empty");
            return Array.Empty<AlbumData>();
        }
        var parentContainer = artists.First().ParentElement;
        var dates = parentContainer!.QuerySelectorAll("div > b");

        Logger.LogInformation("Successfully parsed HTML, found {Albums} albums, {Artist} artists, {Dates} dates", albums.Count, artists.Count, dates.Count());

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
        Logger.LogDebug("Found {Existing} existing albums", existingDbAlbums.Count);
        var changedDbAlbums = existingDbAlbums
            .Where(album => !album.Key.Equals(album.Value))
            .ToList();
        foreach (var (dbAlbum, pageAlbumData) in changedDbAlbums)
        {
            dbAlbum.Update(pageAlbumData);
            Logger.LogTrace("Updated album with id {DbId} {AlbumId}", dbAlbum.Id, dbAlbum.AlbumId);
        }
        Logger.LogDebug("Updated {Changed} albums", changedDbAlbums.Count);

        var newAlbums = pageAlbumsList
            .Where(albumData => !existingDbAlbums.ContainsValue(albumData))
            .Select(albumData => new Album(albumData))
            .ToList();
        dbContext.Albums.AddRange(newAlbums);
        Logger.LogDebug("Added {New} albums", newAlbums.Count);

        await dbContext.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Saved {Changed} changed and {New} new albums to DB", changedDbAlbums.Count, newAlbums.Count);
    }
}
