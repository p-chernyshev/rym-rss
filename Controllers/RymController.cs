using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Models;

namespace RymRss.Controllers;

public class RymController : Controller
{
    private readonly RymRssContext DbContext;
    private readonly ILogger<RymController> Logger;

    public RymController(RymRssContext dbContext, ILogger<RymController> logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    public async Task<IActionResult> Rss()
    {
        var albums = await GetOrderedAlbums();
        var view = View(albums);
        view.ContentType = "application/rss+xml";
        Logger.LogDebug("Generated RSS view for {Count} albums", albums.Count());

        return view;
    }

    public async Task<IActionResult> Atom()
    {
        var albums = await GetOrderedAlbums();
        var view = View(albums);
        view.ContentType = "application/atom+xml";
        Logger.LogDebug("Generated Atom view for {Count} albums", albums.Count());

        return view;
    }

    private async Task<IEnumerable<Album>> GetOrderedAlbums()
    {
        // TODO Execute SQL query on DB
        // .Where(album => album.ReleaseDate >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)))
        return (await DbContext.Albums
                .Include(album => album.Artists)
                .ToListAsync())
            .OrderByDescending(album => album.DateLastChanged);
    }
}
