namespace Roblox.Mssql;

using System;
using System.Data.SqlClient;

using Sentinels;

/// <summary>
/// Execution circuit breaker for MSSQL operations.
/// </summary>
public class MssqlCircuitBreaker : ExecutionCircuitBreakerBase
{
    private readonly TimeSpan _RetryInterval;
    private readonly string _Name;

    /// <inheritdoc cref="CircuitBreakerBase.Name"/>
    protected override string Name => _Name;

    /// <inheritdoc cref="ExecutionCircuitBreakerBase.RetryInterval"/>
    protected override TimeSpan RetryInterval => _RetryInterval;

    /// <summary>
    /// Construct a new instance of <see cref="MssqlCircuitBreaker"/>
    /// </summary>
    /// <param name="database">The <see cref="IDatabase"/></param>
    /// <param name="retryInterval">The retry interval.</param>
    public MssqlCircuitBreaker(IDatabase database, TimeSpan retryInterval)
    {
        _RetryInterval = retryInterval;
        _Name = string.Format("MS SQL Database {0} Circuit Breaker", database.Name);
    }

    /// <inheritdoc cref="ExecutionCircuitBreakerBase.ShouldTrip(Exception)"/>
    protected override bool ShouldTrip(Exception ex)
    {
        if (ex is not SqlException sqlEx) return false;

        return sqlEx.Number == -2 || sqlEx.Number == 11;
    }
}
