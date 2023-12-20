// TODO add installer option to (not) install windows service
// TODO add installer option to install to user startup
// TODO add installer choice install "per-user"/"per-machine"?
// TODO 32/64
// TODO execute installer efbundle silently
// TODO Add feed options: notify about upcoming or only released
// TODO Cross-platform (hide console window?) [windows service, registry]
// TODO Youtube / Twitch
// TODO CI/CD
// TODO UI
// TODO Migrate to WCF
// TODO Log errors to feed
// TODO Warning if no updates after specified period (*week)
// TODO ...universalize
// TODO ...support ajax / js

using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using RymRss.Db;
using RymRss.Extensions;
using RymRss.Models.Options;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

builder.SetupConfiguration();

var appOptions = builder.Configuration.GetRequiredSection(nameof(AppOptions)).Get<AppOptions>()!;

builder.Services.AddControllersWithViews();
builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetRequiredSection(nameof(ScrapeOptions)));
builder.Services.Configure<AppOptions>(builder.Configuration.GetRequiredSection(nameof(AppOptions)));
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var dbPath = Path.Join(appOptions.DataFolder, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddHostedService<RymScraper>();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddWindowsService(options => options.ServiceName = "RYM RSS Service");
}

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

app.Run($"http://localhost:{appOptions.Port}");
