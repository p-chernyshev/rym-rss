using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
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

var installFolderPath = builder.Configuration.GetValue<string?>($"{nameof(AppOptions)}:{nameof(AppOptions.InstallFolder)}") ?? Environment.CurrentDirectory;
installFolderPath = Environment.ExpandEnvironmentVariables(installFolderPath);
var username = builder.Configuration.GetValue<string>($"{nameof(ScrapeOptions)}:{nameof(ScrapeOptions.User)}");
var cookies = builder.Configuration.GetSection($"{nameof(ScrapeOptions)}:{nameof(ScrapeOptions.Cookies)}").Get<string[]>();
var defaultSettingsFilePath = Path.Join(installFolderPath, "appsettings.Default.json");
var settingsFileText = File.Exists(defaultSettingsFilePath)
    ? File.ReadAllText(defaultSettingsFilePath)
    : null;
builder.Configuration.AddJsonFile(defaultSettingsFilePath, true);
var dataFolderPath = builder.Configuration.GetValue<string?>($"{nameof(AppOptions)}:{nameof(AppOptions.DataFolder)}");
if (dataFolderPath != null)
{
    dataFolderPath = Environment.ExpandEnvironmentVariables(dataFolderPath);
    Directory.CreateDirectory(dataFolderPath);
    var settingsFilePath = Path.Join(dataFolderPath, "appsettings.json");
    if (settingsFileText != null && !File.Exists(settingsFilePath))
    {
        var settingsJson = JsonNode.Parse(settingsFileText)!;
        var scrapeOptionsJson = settingsJson[nameof(ScrapeOptions)]!;
        if (username != null) scrapeOptionsJson[nameof(ScrapeOptions.User)] = username;
        if (cookies != null) scrapeOptionsJson[nameof(ScrapeOptions.Cookies)] = new JsonArray(cookies.Select(cookie => (JsonNode?)cookie).ToArray());

        File.WriteAllText(settingsFilePath, settingsJson.ToJsonString(new JsonSerializerOptions {WriteIndented = true}));
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var fileInfo = new FileInfo(settingsFilePath);
            var fileSecurity = fileInfo.GetAccessControl();
            fileSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.FullControl, AccessControlType.Allow));
            fileInfo.SetAccessControl(fileSecurity);
        }
    }
    builder.Configuration.AddJsonFile(settingsFilePath, true);
}
var extraConfig = new Dictionary<string, string?>{ [$"{nameof(AppOptions)}:{nameof(AppOptions.InstallFolder)}"] = installFolderPath };
if (dataFolderPath != null) extraConfig[$"{nameof(AppOptions)}:{nameof(AppOptions.DataFolder)}"] = dataFolderPath;
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
