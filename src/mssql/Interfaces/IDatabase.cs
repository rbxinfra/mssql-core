namespace Roblox.Mssql;

using System;
using System.Data;
using System.Threading;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Base interface for databases.
/// </summary>
public interface IDatabase : IDisposable
{
    /// <summary>
    /// Invoked when database execution fails.
    /// </summary>
    event ExecutionFailedEventHandler ExecutionFailed;

    /// <summary>
    /// Invoked when database execution finishes
    /// </summary>
    event ExecutionFinishedEventHandler ExecutionFinished;

    /// <summary>
    /// Invoked when database execution is started.
    /// </summary>
    event ExecutionStartedEventHandler ExecutionStarted;

    /// <summary>
    /// Invoked when database execution succeeds
    /// </summary>
    event ExecutionSucceededEventHandler ExecutionSucceeded;

    /// <summary>
    /// Gets the connection string.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Gets the connection string with asynchronous operations enabled.
    /// </summary>
    string ConnectionStringAsync { get; }

    /// <summary>
    /// Gets the name of this database.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get the connection string to be utilized by the database.
    /// </summary>
    /// <param name="applicationIntent">The application intent.</param>
    /// <returns>The connection string.</returns>
    string GetUtilizedConnectionString(ApplicationIntent? applicationIntent);

    /// <summary>
    /// Get the connection string with asynchronous operations enabled to be utilized by the database.
    /// </summary>
    /// <param name="applicationIntent">The application intent.</param>
    /// <returns>The connection string.</returns>
    string GetUtilizedConnectionStringAsync(ApplicationIntent? applicationIntent);

    /// <summary>
    /// Execute a command and return the number of rows affected.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>Number of rows affected</returns>
    int ExecuteNonQuery(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null);

    /// <summary>
    /// Execute a command and return the number of rows affected.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>Number of rows affected</returns>
    Task<int> ExecuteNonQueryAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null);

    /// <summary>
    /// Execute a command and return an SQL reader.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>An SQL Data Reader</returns>
    IEnumerable<IDictionary<string, object>> ExecuteReader(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null);

    /// <summary>
    /// Execute a command and return an SQL reader.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>An SQL Data Reader</returns>
    Task<IEnumerable<IDictionary<string, object>>> ExecuteReaderAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null);

    /// <summary>
    /// Executes a command and returns the first column of the first row.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>The first column of the first row.</returns>
    object ExecuteScalar(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null);

    /// <summary>
    /// Executes a command and returns the first column of the first row.
    /// </summary>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="commandType">The command type</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application intent</param>
    /// <returns>The first column of the first row.</returns>
    Task<object> ExecuteScalarAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null);
}
