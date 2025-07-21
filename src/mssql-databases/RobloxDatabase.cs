namespace Roblox.MssqlDatabases;

/// <summary>
/// Roblox Databases
/// </summary>
/// <remarks>
/// This is a list of all the databases that Roblox uses.
/// Please also update <see cref="DatabaseConnectionStrings"/>
/// and <see cref="TimeoutSettings"/>
/// </remarks>
public enum RobloxDatabase
{
    /// <summary>
    /// The Roblox Devices database.
    /// </summary>
    RobloxDevices,

    /// <summary>
    /// The Roblox Services database.
    /// </summary>
    RobloxServices,

    /// <summary>
    /// The Roblox Throttling database.
    /// </summary>
    RobloxThrottling,

    /// <summary>
    /// The Roblox Membership database.
    /// </summary>
    RobloxUsers,
    
    /// <summary>
    /// The Roblox Roles database.
    /// </summary>
    RobloxRoles,
    
    /// <summary>
    /// The Roblox Email Addresses database.
    /// </summary>
    RobloxEmailAddresses,

    /// <summary>
    /// The Roblox Ip Addresses database.
    /// </summary>
    RobloxIpAddresses,

    /// <summary>
    /// The Roblox Mac Addresses database.
    /// </summary>
    RobloxMacAddresses,

    /// <summary>
    /// The Roblox Leased Locks database.
    /// </summary
    RobloxLeasedLocks,

    /// <summary>
    /// Test database.
    /// </summary>
    TestDatabase
}
