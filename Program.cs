using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using RymRss.Db;
using RymRss.Models.Options;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
var appFolderPath = Path.Join(appDataPath, "RymRss");
Directory.CreateDirectory(appFolderPath);

string? settingsFolderPath = null;
using (var key = Registry.LocalMachine.OpenSubKey(@"Software\RymRss"))
{
    if (key?.GetValue("SettingsFolder") is string installFolderKey)
    {
        settingsFolderPath = installFolderKey;
    }
}

if (settingsFolderPath is not null)
{
    builder.Configuration.AddJsonFile(Path.Join(settingsFolderPath, "appsettings.json"));
}
builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AppOptions:SettingsFolder"] = settingsFolderPath,
        ["AppOptions:DataFolder"] = appFolderPath,
    });
var appOptions = builder.Configuration.GetRequiredSection(nameof(AppOptions)).Get<AppOptions>();

builder.Services.AddControllersWithViews();
builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetRequiredSection(nameof(ScrapeOptions)));
builder.Services.Configure<AppOptions>(builder.Configuration.GetRequiredSection(nameof(AppOptions)));
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var dbPath = Path.Join(appFolderPath, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddHostedService<RymScraper>();
builder.Services.AddWindowsService(options => options.ServiceName = "Rym Rss Service");

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
