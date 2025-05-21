namespace Roblox.MssqlDatabases;

using System;
using System.Data;
using System.Threading;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Mssql;
using Instrumentation;

/// <summary>
/// Extension methods for <see cref="RobloxDatabase"/>
/// </summary>
public static class Extensions
{
    private static readonly ConcurrentDictionary<RobloxDatabase, Func<string>> _ConnectionStringGetterCache = new();
    private static readonly DatabaseRegistry _DatabaseRegistry = new(BuildDatabaseObserver);
    private static readonly DatabaseConnectionStrings _ConnectionStrings = DatabaseConnectionStrings.Singleton;
    private static readonly Type _ConnectionStringsType = _ConnectionStrings.GetType();
    private static readonly ConcurrentDictionary<RobloxDatabase, Func<TimeSpan>> _CommandTimeoutGetterCache = new();
    private static readonly TimeoutSettings _TimeoutSettings = TimeoutSettings.Singleton;
    private static readonly Type _TimeoutSettingsType = _TimeoutSettings.GetType();
    private static readonly Func<TimeSpan> _DefaultDatabaseTimeoutGetter = () => _TimeoutSettings.DefaultCommandTimeout;

    /// <summary>
    /// Get the connection string getter for the specified <see cref="RobloxDatabase"/>
    /// </summary>
    /// <param name="robloxDatabase">The database.</param>
    /// <returns>The connection string getter.</returns>
    /// <exception cref="Exception">ConnectionString for database is not defined.</exception>
    public static Func<string> GetConnectionStringGetter(this RobloxDatabase robloxDatabase)
    {
        return _ConnectionStringGetterCache.GetOrAdd(robloxDatabase, db =>
        {
            var dbName = db.ToString();
            var csProp = _ConnectionStringsType.GetProperty(dbName) ?? throw new Exception($"ConnectionString for database {dbName} is not defined.");

            var getter = csProp.GetGetMethod();
            var rawGetter = (Func<DatabaseConnectionStrings, string>)getter.CreateDelegate(typeof(Func<DatabaseConnectionStrings, string>));
            return () => rawGetter(_ConnectionStrings);
        });
    }

    /// <summary>
    /// Get the command timeout getter for the specified <see cref="RobloxDatabase"/>
    /// </summary>
    /// <param name="robloxDatabase">The database.</param>
    /// <returns>The command timeout getter.</returns>
    public static Func<TimeSpan> GetCommandTimeoutGetter(this RobloxDatabase robloxDatabase)
    {
        return _CommandTimeoutGetterCache.GetOrAdd(robloxDatabase, db =>
        {
            var dbName = db.ToString();
            var csProp = _TimeoutSettingsType.GetProperty(dbName);
            if (csProp == null)
                return _DefaultDatabaseTimeoutGetter;

            var getter = csProp.GetGetMethod();
            var rawGetter = (Func<TimeoutSettings, TimeSpan>)getter.CreateDelegate(typeof(Func<TimeoutSettings, TimeSpan>));
            return () => rawGetter(_TimeoutSettings);
        });
    }

    /// <summary>
    /// Validate the existence of the <see cref="RobloxDatabase"/>
    /// </summary>
    /// <param name="robloxDatabase">The database.</param>
    /// <exception cref="KeyNotFoundException">Unknown database.</exception>
    public static void ValidateDatabase(this RobloxDatabase robloxDatabase) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase);

    /// <inheritdoc cref="IDatabase.ExecuteNonQuery(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public static int ExecuteNonQuery(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure,
        int? commandTimeout = null, 
        ApplicationIntent? applicationIntent = null
    ) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteNonQuery(
                commandText, 
                sqlParameters, 
                commandType, 
                commandTimeout, 
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ExecuteNonQueryAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public static Task<int> ExecuteNonQueryAsync(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure,
        CancellationToken cancellationToken = default(CancellationToken), 
        int? commandTimeout = null, 
        ApplicationIntent? applicationIntent = null
    )
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteNonQueryAsync(
                commandText, 
                sqlParameters, 
                commandType, 
                cancellationToken, 
                commandTimeout, 
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ExecuteReader(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public static IEnumerable<IDictionary<string, object>> ExecuteReader(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure, 
        int? commandTimeout = null, 
        ApplicationIntent? applicationIntent = null
    ) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteReader(
                commandText, 
                sqlParameters, 
                commandType, 
                commandTimeout, 
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ExecuteReaderAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public static Task<IEnumerable<IDictionary<string, object>>> ExecuteReaderAsync(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure, 
        CancellationToken cancellationToken = default(CancellationToken), 
        int? commandTimeout = null, 
        ApplicationIntent? applicationIntent = null
    ) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteReaderAsync(
                commandText, 
                sqlParameters, 
                commandType, 
                cancellationToken, 
                commandTimeout,
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ExecuteScalar(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public static object ExecuteScalar(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure,
        int? commandTimeout = null, 
        ApplicationIntent? applicationIntent = null
    ) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteScalar(
                commandText, 
                sqlParameters, 
                commandType, 
                commandTimeout, 
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ExecuteScalarAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public static Task<object> ExecuteScalarAsync(
        this RobloxDatabase robloxDatabase, 
        string commandText, 
        SqlParameter[] sqlParameters, 
        CommandType commandType = CommandType.StoredProcedure, 
        CancellationToken cancellationToken = default(CancellationToken),
        int? commandTimeout = null,
        ApplicationIntent? applicationIntent = null
    ) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ExecuteScalarAsync(
                commandText, 
                sqlParameters, 
                commandType, 
                cancellationToken, 
                commandTimeout, 
                applicationIntent
           );

    /// <inheritdoc cref="IDatabase.ConnectionString"/>
    public static string GetConnectionString(this RobloxDatabase robloxDatabase) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ConnectionString;

    /// <inheritdoc cref="IDatabase.ConnectionStringAsync"/>
    public static string GetConnectionStringForAsync(this RobloxDatabase robloxDatabase) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).ConnectionStringAsync;

    /// <inheritdoc cref="Database.CommandTimeout"/>
    public static TimeSpan GetCommandTimeout(this RobloxDatabase robloxDatabase) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).CommandTimeout;

    /// <inheritdoc cref="IDatabase.GetUtilizedConnectionString(ApplicationIntent?)"/>
    public static string GetUtilizedConnectionString(this RobloxDatabase robloxDatabase, ApplicationIntent? applicationIntent) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).GetUtilizedConnectionString(applicationIntent);

    /// <inheritdoc cref="IDatabase.GetUtilizedConnectionStringAsync(ApplicationIntent?)"/>
    public static string GetUtilizedConnectionStringForAsync(this RobloxDatabase robloxDatabase, ApplicationIntent? applicationIntent) 
        => _DatabaseRegistry.GetDatabase(robloxDatabase).GetUtilizedConnectionStringAsync(applicationIntent);

    private static IDatabaseObserver BuildDatabaseObserver(IDatabase database) 
        => new DatabaseObserver(database, StaticCounterRegistry.Instance);
}
