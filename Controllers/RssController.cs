using System.Diagnostics;
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
        var albums = (await DbContext.Albums.ToListAsync())
            .OrderByDescending(album => album.DateLastChanged);
        return RssView(albums);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private ViewResult RssView<TModel>(TModel? model)
    {
        var view = View(model);
        view.ContentType = "application/rss+xml";
        return view;
    }
}
