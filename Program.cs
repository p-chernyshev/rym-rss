using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using RymRss.Configuration;
using RymRss.Db;
using RymRss.Models.Options;
using RymRss.Services;

var builder = WebApplication.CreateBuilder(args);

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Configuration.AddRegistryKey(Registry.LocalMachine, @"Software\RymRss");
}
var settingsFolderPath = builder.Configuration.GetValue<string?>("AppOptions:SettingsFolder");
var username = builder.Configuration.GetValue<string?>("ScrapeOptions:User");
if (settingsFolderPath is not null)
{
    var settingsFilePath = Path.Join(settingsFolderPath, "appsettings.json");
    if (username is not null && File.Exists(settingsFilePath))
    {
        var settingsFileText = File.ReadAllText(settingsFilePath);
        settingsFileText = settingsFileText.Replace("{{RYM_USERNAME}}", username);
        File.WriteAllText(settingsFilePath, settingsFileText);
    }
    builder.Configuration.AddJsonFile(settingsFilePath, true);
}
var dataFolderPath = builder.Configuration.GetValue<string?>("AppOptions:DataFolder");
var extraConfig = new Dictionary<string, string?>();
if (settingsFolderPath is not null) extraConfig["AppOptions:SettingsFolder"] = Environment.ExpandEnvironmentVariables(settingsFolderPath);
if (dataFolderPath is not null) extraConfig["AppOptions:DataFolder"] = Environment.ExpandEnvironmentVariables(dataFolderPath);
builder.Configuration.AddInMemoryCollection(extraConfig);

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

app.Run($"http://localhost:{appOptions.Port}");
