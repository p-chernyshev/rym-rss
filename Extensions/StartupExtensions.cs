using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using RymRss.Configuration;
using RymRss.Models.Options;

namespace RymRss.Extensions;

public static class StartupExtensions
{
    public static void SetupConfiguration(this WebApplicationBuilder builder)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            builder.Configuration.AddRegistryKey(Registry.LocalMachine, @"Software\RymRss");
        }

        var installFolderPath = builder.Configuration.GetValue<string?>($"{nameof(AppOptions)}:{nameof(AppOptions.InstallFolder)}") ?? Environment.CurrentDirectory;
        installFolderPath = Environment.ExpandEnvironmentVariables(installFolderPath);
        var defaultSettingsFilePath = Path.Join(installFolderPath, "appsettings.Default.json");
        var defaultSettingsJson = File.Exists(defaultSettingsFilePath)
            ? File.ReadAllText(defaultSettingsFilePath)
            : null;
        builder.Configuration.AddJsonFile(defaultSettingsFilePath, true);
        var dataFolderPath = builder.Configuration.GetValue<string?>($"{nameof(AppOptions)}:{nameof(AppOptions.DataFolder)}");
        if (dataFolderPath != null)
        {
            dataFolderPath = Environment.ExpandEnvironmentVariables(dataFolderPath);
            Directory.CreateDirectory(dataFolderPath);
            var settingsFilePath = Path.Join(dataFolderPath, "appsettings.json");

            InitializeUserSettingsFile(defaultSettingsJson, settingsFilePath, builder.Configuration);

            builder.Configuration.AddJsonFile(settingsFilePath, true);
        }
        var extraConfig = new Dictionary<string, string?>{ [$"{nameof(AppOptions)}:{nameof(AppOptions.InstallFolder)}"] = installFolderPath };
        if (dataFolderPath != null) extraConfig[$"{nameof(AppOptions)}:{nameof(AppOptions.DataFolder)}"] = dataFolderPath;
        builder.Configuration.AddInMemoryCollection(extraConfig);
    }

    private static void InitializeUserSettingsFile(
        string? defaultSettingsJson,
        string settingsFilePath,
        IConfiguration configuration)
    {
        if (defaultSettingsJson == null || File.Exists(settingsFilePath)) return;
        var settingsJson = JsonNode.Parse(defaultSettingsJson)!;
        var scrapeOptionsJson = settingsJson[nameof(ScrapeOptions)]!;
        var username = configuration.GetValue<string>($"{nameof(ScrapeOptions)}:{nameof(ScrapeOptions.User)}");
        var cookies = configuration.GetSection($"{nameof(ScrapeOptions)}:{nameof(ScrapeOptions.Cookies)}").Get<string[]>();
        if (username != null) scrapeOptionsJson[nameof(ScrapeOptions.User)] = username;
        if (cookies != null) scrapeOptionsJson[nameof(ScrapeOptions.Cookies)] = new JsonArray(cookies.Select(cookie => (JsonNode?)cookie).ToArray());
        File.WriteAllText(settingsFilePath, settingsJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var fileInfo = new FileInfo(settingsFilePath);
        var fileSecurity = fileInfo.GetAccessControl();
        fileSecurity.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
            FileSystemRights.FullControl, AccessControlType.Allow));
        fileInfo.SetAccessControl(fileSecurity);
    }
}
