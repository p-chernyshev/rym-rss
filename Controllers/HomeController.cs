using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RymRss.Models;

namespace RymRss.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return XmlView();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private ViewResult XmlView()
    {
        var view = View();
        view.ContentType = "text/xml";
        return view;
    }
}
