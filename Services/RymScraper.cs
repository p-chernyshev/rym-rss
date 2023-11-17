using System.Globalization;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Io.Cookie;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RymRss.Db;
using RymRss.Extensions;
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

    private IEnumerable<PageAlbumData> ExtractPageAlbumData(IDocument document)
    {
        var upcomingHeader = document.QuerySelectorAll("th").Single(element => element.Text().Contains("Upcoming"));
        var upcomingListBlock = upcomingHeader.ParentElement?.NextElementSibling?.FirstElementChild?.FirstElementChild;
        if (upcomingListBlock is null) throw new Exception("Couldn't find upcoming albums list on page");
        var upcomingListAlbumGroups = upcomingListBlock.Children
            .SplitBy(element => element.TagName.ToLower() == "br")
            .ToList();
        if (!upcomingListAlbumGroups.Any())
        {
            Logger.LogInformation("Parsed list of albums is empty");
            return Array.Empty<PageAlbumData>();
        }
        var dates = upcomingListBlock.QuerySelectorAll("div > b");

        var albumElements = upcomingListAlbumGroups
            .Select(group =>
            {
                var groupElements = group.ToList();
                var artists = groupElements
                    .OfType<IHtmlAnchorElement>()
                    .Where(element => element.ClassList.Contains("artist"))
                    .Concat(
                        groupElements.SelectMany(element => element.QuerySelectorAll<IHtmlAnchorElement>(".artist")))
                    .ToList();
                var album = groupElements
                    .OfType<IHtmlAnchorElement>()
                    .Single(element => element.ClassList.Contains("album"));
                var date = dates.Last(date => date.CompareDocumentPosition(album).HasFlag(DocumentPositions.Following));
                return new { album, artists, date };
            })
            .ToList();

        Logger.LogInformation("Successfully parsed HTML, found {CountAlbums} albums with {CountDates} dates", albumElements.Count, dates.Length);

        return albumElements.Select(pageElements => new PageAlbumData
        {
            Title = pageElements.album.TextContent,
            Id = pageElements.album.Title ?? "",
            Href = pageElements.album.Href,
            Artists = pageElements.artists
                .Select(artist => new ArtistData
                {
                    Name = artist.TextContent,
                    Id = artist.Title ?? "",
                    Href = artist.Href,
                })
                .ToList(),
            ReleaseDate = DateOnly.Parse(pageElements.date.TextContent, RymCulture),
        });
    }

    private async Task UpdateDbAlbumData(RymRssContext dbContext, IEnumerable<PageAlbumData> pageAlbums, CancellationToken cancellationToken)
    {
        var pageAlbumsList = pageAlbums.ToList();

        var allPageArtists = pageAlbumsList
            .SelectMany(album => album.Artists)
            .DistinctBy(artist => artist.Id)
            .ToDictionary(artist => artist.Id, artist => artist);

        var existingDbArtists = await dbContext.Artists
            .Where(artist => allPageArtists.Keys.Contains(artist.Id))
            .ToDictionaryAsync(
                artist => allPageArtists[artist.Id],
                artist => artist,
                cancellationToken);
        Logger.LogDebug("Found {CountExisting} existing artists", existingDbArtists.Count);

        var changedDbArtists = existingDbArtists
            .Where(artist => !artist.Value.Equals(artist.Key))
            .ToList();
        foreach (var (pageArtistData, dbArtist) in changedDbArtists)
        {
            dbArtist.Update(pageArtistData);
            Logger.LogTrace("Updated artist with id {ArtistId}", dbArtist.Id);
        }
        Logger.LogDebug("Updated {CountChanged} artists", changedDbArtists.Count);

        var newArtists = allPageArtists.Values
            .Where(artistData => !existingDbArtists.ContainsKey(artistData))
            .Select(artistData => new Artist(artistData))
            .ToList();
        dbContext.Artists.AddRange(newArtists);
        Logger.LogDebug("Added {CountNew} artists", newArtists.Count);

        var allDbArtists = existingDbArtists.Values.Concat(newArtists).ToList();

        var pageAlbumIds = pageAlbumsList.Select(albumData => albumData.Id).ToList();
        var existingDbAlbums = await dbContext.Albums
            .Where(album => pageAlbumIds.Contains(album.Id))
            .Include(album => album.Artists)
            .ToDictionaryAsync(
                album => pageAlbumsList.First(albumData => albumData.Id == album.Id),
                album => album,
                cancellationToken);
        Logger.LogDebug("Found {CountExisting} existing albums", existingDbAlbums.Count);

        var changedDbAlbums = existingDbAlbums
            .Where(album => !album.Value.Equals(album.Key))
            .ToList();
        foreach (var (pageAlbumData, dbAlbum) in changedDbAlbums)
        {
            dbAlbum.Update(pageAlbumData, allDbArtists);
            Logger.LogTrace("Updated album with id {AlbumId}", dbAlbum.Id);
        }
        Logger.LogDebug("Updated {CountChanged} albums", changedDbAlbums.Count);

        var newAlbums = pageAlbumsList
            .Where(albumData => !existingDbAlbums.ContainsKey(albumData))
            .Select(albumData => new Album(albumData, allDbArtists))
            .ToList();
        dbContext.Albums.AddRange(newAlbums);
        Logger.LogDebug("Added {CountNew} albums", newAlbums.Count);

        await dbContext.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Saved {CountChanged} changed and {CountNew} new albums to DB", changedDbAlbums.Count, newAlbums.Count);
    }
}
