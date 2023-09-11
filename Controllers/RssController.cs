using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Models;

namespace RymRss.Controllers;

public class RssController : Controller
{
    private readonly RymRssContext DbContext;

    public RssController(RymRssContext dbContext)
    {
        DbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var albums = await GetOrderedAlbums();
        var view = View(albums);
        view.ContentType = "application/rss+xml";
        return view;
    }

    public async Task<IActionResult> Atom()
    {
        var albums = await GetOrderedAlbums();
        var view = View(albums);
        view.ContentType = "application/atom+xml";
        return view;
    }

    private async Task<IEnumerable<Album>> GetOrderedAlbums()
    {
        // TODO Execute SQL query on DB
        return (await DbContext.Albums.ToListAsync())
            .OrderByDescending(album => album.DateLastChanged);
    }
}
