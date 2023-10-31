using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using RymRss.Db;
using RymRss.Models.Options;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

string? dataFolderPath = null;
string? username = null;
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    using var key = Registry.LocalMachine.OpenSubKey(@"Software\RymRss");
    if (key?.GetValue("AppOptions:DataFolder") is string dataFolderValue) dataFolderPath = dataFolderValue;
    if (key?.GetValue("ScrapeOptions:User") is string userValue) username = userValue;
}

if (dataFolderPath is not null)
{
    var settingsFilePath = Path.Join(dataFolderPath, "appsettings.json");
    if (username is not null)
    {
        var settingsFileText = File.ReadAllText(settingsFilePath);
        settingsFileText = settingsFileText.Replace("{{RYM_USERNAME}}", username);
        File.WriteAllText(settingsFilePath, settingsFileText);
    }
    builder.Configuration.AddJsonFile(settingsFilePath);
}
builder.Configuration
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["AppOptions:DataFolder"] = dataFolderPath,
    });
var appOptions = builder.Configuration.GetRequiredSection(nameof(AppOptions)).Get<AppOptions>();

builder.Services.AddControllersWithViews();
builder.Services.Configure<ScrapeOptions>(builder.Configuration.GetRequiredSection(nameof(ScrapeOptions)));
builder.Services.Configure<AppOptions>(builder.Configuration.GetRequiredSection(nameof(AppOptions)));
builder.Services.AddDbContext<RymRssContext>(options =>
{
    var dbPath = Path.Join(dataFolderPath, "rymrss.db");
    options.UseSqlite($"Data Source={dbPath}");
});
builder.Services.AddHostedService<RymScraper>();
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddWindowsService(options => options.ServiceName = "Rym Rss Service");
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

app.Run($"http://localhost:{appOptions!.Port}");
