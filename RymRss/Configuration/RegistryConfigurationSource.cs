#pragma warning disable CA1416
using Microsoft.Win32;

namespace RymRss.Configuration;

public class RegistryConfigurationSource : IConfigurationSource
{
    private readonly RegistryKey RegistryRootKey;
    private readonly string SubKeyName;

    public RegistryConfigurationSource(RegistryKey rootKey, string subKeyName) =>
        (RegistryRootKey, SubKeyName) = (rootKey, subKeyName);

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new RegistryConfigurationProvider(RegistryRootKey.OpenSubKey(SubKeyName));
}
