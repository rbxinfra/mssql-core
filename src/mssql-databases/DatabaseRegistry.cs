namespace Roblox.MssqlDatabases;

using System;
using System.Collections.Generic;

using Mssql;

/// <summary>
/// <see cref="RobloxDatabase"/> registry.
/// </summary>
public sealed class DatabaseRegistry : IDisposable
{
    private static readonly TimeSpan _DefaultRetryInterval = TimeSpan.FromMilliseconds(100);

    private readonly Func<IDatabase, IDatabaseObserver> _DatabaseObserverBuilder;
    private readonly Dictionary<RobloxDatabase, Lazy<MonitoredGuardedDatabase>> _Databases = new();

    private bool _IsDisposed;

    /// <summary>
    /// Construct a new instance of <see cref="DatabaseRegistry"/>
    /// </summary>
    /// <param name="databaseObserverBuilder">The <see cref="IDatabaseObserver"/> builder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseObserverBuilder"/> cannot be null.</exception>
    public DatabaseRegistry(Func<IDatabase, IDatabaseObserver> databaseObserverBuilder)
    {
        _DatabaseObserverBuilder = databaseObserverBuilder ?? throw new ArgumentNullException(nameof(databaseObserverBuilder));

        var databases = (RobloxDatabase[])Enum.GetValues(typeof(RobloxDatabase));
        for (int i = 0; i < databases.Length; i++)
        {
            var database = databases[i];

            _Databases.Add(database, new Lazy<MonitoredGuardedDatabase>(() => BuildDatabase(database)));
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        if (_IsDisposed) return;
        foreach (var kvp in _Databases)
            if (kvp.Value.IsValueCreated)
                kvp.Value.Value?.Dispose();

        _IsDisposed = true;
    }

    /// <summary>
    /// Get the <see cref="Database"/> for the specified <see cref="RobloxDatabase"/>
    /// </summary>
    /// <param name="robloxDatabase">The database</param>
    /// <returns>The database class</returns>
    /// <exception cref="KeyNotFoundException">Unknown database</exception>
    public Database GetDatabase(RobloxDatabase robloxDatabase)
    {
        if (!_Databases.TryGetValue(robloxDatabase, out var db))
            throw new KeyNotFoundException("Unknown database: " + robloxDatabase);

        return db.Value;
    }

    private MonitoredGuardedDatabase BuildDatabase(RobloxDatabase robloxDatabase)
    {
        return new MonitoredGuardedDatabase(
            robloxDatabase.ToString(),
            robloxDatabase.GetConnectionStringGetter(),
            robloxDatabase.GetCommandTimeoutGetter(), 
            _DefaultRetryInterval, 
            _DatabaseObserverBuilder
        );
    }
}
