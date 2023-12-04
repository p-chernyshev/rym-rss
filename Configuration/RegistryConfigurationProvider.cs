#pragma warning disable CA1416
using Microsoft.Win32;

namespace RymRss.Configuration;

public class RegistryConfigurationProvider : ConfigurationProvider
{

    private readonly RegistryKey? ConfigRootKey;

    public RegistryConfigurationProvider(RegistryKey? configRootKey) =>
        ConfigRootKey = configRootKey;

    public override void Load()
    {
        if (ConfigRootKey is not null) LoadKeyWithSubKeys(ConfigRootKey);
    }

    private static string AddPrefix(string? prefix, string? value)
    {
        return (prefix, value) switch
        {
            (null or "", null or "") => string.Empty,
            (null or "", _) => value,
            (_, null or "") => prefix,
            _ => $"{prefix}:{value}",
        };
    }

    private void LoadKeyWithSubKeys(RegistryKey registryKey, string? prefix = null)
    {
        foreach (var valueName in registryKey.GetValueNames())
        {
            LoadValue(registryKey, prefix, valueName);
        }

        foreach (var subKeyName in registryKey.GetSubKeyNames())
        {
            LoadKeyWithSubKeys(registryKey.OpenSubKey(subKeyName)!, AddPrefix(prefix, subKeyName));
        }
    }

    private void LoadValue(RegistryKey registryKey, string? prefix, string? valueName)
    {
        var prefixedName = AddPrefix(prefix, valueName);
        var value = registryKey.GetValue(valueName);
        if (value is null) return;
        var kind = registryKey.GetValueKind(valueName);
        switch (kind)
        {
            case RegistryValueKind.String:
            case RegistryValueKind.ExpandString:
                var stringValue = (string)value;
                Data[prefixedName] = stringValue;
                break;
            case RegistryValueKind.MultiString:
                var multiStringValue = ((string[])value)
                    .SelectMany(singleString => singleString.Split(new []{"\r\n", "\n"}, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                    .ToList();
                for (var i = 0; i < multiStringValue.Count; i++)
                {
                    Data[AddPrefix(prefixedName, i.ToString())] = multiStringValue[i];
                }
                break;
            case RegistryValueKind.Binary:
                var binaryValue = (byte[])value;
                Data[prefixedName] = Convert.ToBase64String(binaryValue);
                break;
            case RegistryValueKind.DWord:
                var dwordValue = (int)value;
                Data[prefixedName] = dwordValue.ToString();
                break;
            case RegistryValueKind.QWord:
                var qwordValue = (long)value;
                Data[prefixedName] = qwordValue.ToString();
                break;
            default:
                throw new Exception("Unknown registry value kind");
        }
    }
}
