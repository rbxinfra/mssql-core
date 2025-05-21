namespace Roblox.MssqlDatabases;

using System;

/// <summary>
/// Default details for the settings providers.
/// </summary>
internal static class SettingsProvidersDefaults
{
    /// <summary>
    /// The path prefix for the data access platform.
    /// </summary>
    public const string ProviderPathPrefix = "data-access-platform";

    /// <summary>
    /// The path to the connection strings.
    /// </summary>
    public const string ConnectionStringsPath = $"{ProviderPathPrefix}/database-connection-strings";

    /// <summary>
    /// The path to the timeout settings.
    /// </summary>
    public const string TimeoutSettingsPath = $"{ProviderPathPrefix}/database-timeout-settings";
}
