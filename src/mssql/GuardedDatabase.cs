namespace Roblox.Mssql;

using System;
using System.Data;
using System.Threading;
using System.Data.SqlClient;
using System.Threading.Tasks;

/// <summary>
/// A version of Database that is guarded by a circuit breaker.
/// </summary>
public class GuardedDatabase : Database
{
    private readonly MssqlCircuitBreaker _CircuitBreaker;

    /// <summary>
    /// Construct a new instance of <see cref="GuardedDatabase"/>
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <param name="connectionStringGetter">A function that returns the connection string.</param>
    /// <param name="commandTimeoutGetter">A function that returns the command timeout.</param>
    /// <param name="retryInterval">The retry interval for the circuit breaker.</param>
    /// <exception cref="ArgumentException">Invalid value for argument <paramref name="name"/>.</exception>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="connectionStringGetter"/> cannot be null.
    /// - <paramref name="commandTimeoutGetter"/> cannot be null.
    /// </exception>
    public GuardedDatabase(string name, Func<string> connectionStringGetter, Func<TimeSpan> commandTimeoutGetter, TimeSpan retryInterval) 
        : base(name, connectionStringGetter, commandTimeoutGetter)
    {
        _CircuitBreaker = new MssqlCircuitBreaker(this, retryInterval);
    }

    /// <inheritdoc cref="Database.Execute(CommandType, string, SqlParameter[], Action{SqlCommand}, int?, ApplicationIntent?)"/>
    protected new void Execute(CommandType commandType, string commandText, SqlParameter[] sqlParameters, Action<SqlCommand> action, int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
        => _CircuitBreaker.Execute(() => base.Execute(commandType, commandText, sqlParameters, action, commandTimeout, applicationIntent));

    /// <inheritdoc cref="Database.ExecuteAsync(CommandType, string, SqlParameter[], Func{SqlCommand, CancellationToken, Task}, CancellationToken, int?, ApplicationIntent?)"/>
    protected new async Task ExecuteAsync(CommandType commandType, string commandText, SqlParameter[] sqlParameters, Func<SqlCommand, CancellationToken, Task> action, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
        => await _CircuitBreaker.ExecuteAsync(async (ct) => await base.ExecuteAsync(commandType, commandText, sqlParameters, action, ct, commandTimeout, applicationIntent), cancellationToken);
}
