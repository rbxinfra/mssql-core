namespace Roblox.MssqlDatabases;

using Configuration;

/// <summary>
/// Settings provider for database connection strings
/// </summary>
public class DatabaseConnectionStrings : BaseSettingsProvider<DatabaseConnectionStrings>
{
    /// <inheritdoc cref="IVaultProvider.Path"/>
    protected override string ChildPath => SettingsProvidersDefaults.ConnectionStringsPath;

    /// <summary>
    /// Gets the connection string for the RobloxDevices database.
    /// </summary>
    [SettingName($"{nameof(RobloxDevices)}ConnectionString")]
    public string RobloxDevices => GetOrDefault($"{nameof(RobloxDevices)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxServices database.
    /// </summary>
    [SettingName($"{nameof(RobloxServices)}ConnectionString")]
    public string RobloxServices => GetOrDefault($"{nameof(RobloxServices)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxUsers database.
    /// </summary>
    [SettingName($"{nameof(RobloxUsers)}ConnectionString")]
    public string RobloxUsers => GetOrDefault($"{nameof(RobloxUsers)}ConnectionString", string.Empty);
    
    /// <summary>
    /// Gets the connection string for the RobloxRoles database.
    /// </summary>
    [SettingName($"{nameof(RobloxRoles)}ConnectionString")]
    public string RobloxRoles => GetOrDefault($"{nameof(RobloxRoles)}ConnectionString", string.Empty);
    
    /// <summary>
    /// Gets the connection string for the RobloxEmailAddresses database.
    /// </summary>
    [SettingName($"{nameof(RobloxEmailAddresses)}ConnectionString")]
    public string RobloxEmailAddresses => GetOrDefault($"{nameof(RobloxEmailAddresses)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxIpAddresses database.
    /// </summary>
    [SettingName($"{nameof(RobloxIpAddresses)}ConnectionString")]
    public string RobloxIpAddresses => GetOrDefault($"{nameof(RobloxIpAddresses)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxMacAddresses database.
    /// </summary>
    [SettingName($"{nameof(RobloxMacAddresses)}ConnectionString")]
    public string RobloxMacAddresses => GetOrDefault($"{nameof(RobloxMacAddresses)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxLeasedLocks database.
    /// </summary>
    [SettingName($"{nameof(RobloxLeasedLocks)}ConnectionString")]
    public string RobloxLeasedLocks => GetOrDefault($"{nameof(RobloxLeasedLocks)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the RobloxNonces database.
    /// </summary>
    [SettingName($"{nameof(RobloxNonces)}ConnectionString")]
    public string RobloxNonces => GetOrDefault($"{nameof(RobloxNonces)}ConnectionString", string.Empty);

    /// <summary>
    /// Gets the connection string for the TestDatabase database.
    /// </summary>
    [SettingName($"{nameof(TestDatabase)}ConnectionString")]
    public string TestDatabase => GetOrDefault($"{nameof(TestDatabase)}ConnectionString", string.Empty);
}
