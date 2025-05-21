namespace Roblox.MssqlDatabases;

using System;

using Configuration;

/// <summary>
/// Settings provider for database timeout settings
/// </summary>
public class TimeoutSettings : BaseSettingsProvider<TimeoutSettings>
{
    /// <inheritdoc cref="IVaultProvider.Path"/>
    protected override string ChildPath => SettingsProvidersDefaults.TimeoutSettingsPath;

    /// <summary>
    /// Gets the default timeout for all databases.
    /// </summary>
    public TimeSpan DefaultCommandTimeout => GetOrDefault(nameof(DefaultCommandTimeout), TimeSpan.FromSeconds(30));
}
