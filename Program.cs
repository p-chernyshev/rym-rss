using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Models.Options;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var appFolderPath = Path.Join(appDataPath, "RymRss");
Directory.CreateDirectory(appFolderPath);
var appOptions = builder.Configuration.GetRequiredSection(nameof(AppOptions)).Get<AppOptions>();

builder.Services.AddControllersWithViews();
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["AppOptions:WorkingFolder"] = appFolderPath,
});
builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetRequiredSection(nameof(ScrapeOptions)));
builder.Services.Configure<AppOptions>(builder.Configuration.GetRequiredSection(nameof(AppOptions)));
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var dbPath = Path.Join(appFolderPath, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddWindowsService(options => options.ServiceName = "Rym Rss Service");
builder.Services.AddHostedService<RymScraper>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Rym}/{action=Rss}");

app.Run($"http://localhost:{appOptions!.Port}");
