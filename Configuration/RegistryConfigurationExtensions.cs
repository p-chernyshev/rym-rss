using Microsoft.Win32;

namespace RymRss.Configuration;

public static class RegistryConfigurationExtensions
{
    public static IConfigurationBuilder AddRegistryKey(this IConfigurationBuilder builder, RegistryKey rootKey, string subKeyName)
    {
        var source = new RegistryConfigurationSource(rootKey, subKeyName);
        return builder.Add(source);
    }
}
