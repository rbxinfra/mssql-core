namespace Roblox.Mssql;

using System;
using System.Data;
using System.Threading;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Base class for all MSSQL databases.
/// </summary>
public class Database : IDatabase
{
    private readonly Func<string> _ConnectionStringGetter;
    private readonly Func<TimeSpan> _CommandTimeoutGetter;
    private long _RequestCounter;

    private int _CommandTimeoutInSeconds => (int)Math.Ceiling(CommandTimeout.TotalSeconds);

    /// <inheritdoc cref="IDatabase.ExecutionFailed"/>
    public event ExecutionFailedEventHandler ExecutionFailed;

    /// <inheritdoc cref="IDatabase.ExecutionFinished"/>
    public event ExecutionFinishedEventHandler ExecutionFinished;

    /// <inheritdoc cref="IDatabase.ExecutionStarted"/>
    public event ExecutionStartedEventHandler ExecutionStarted;

    /// <inheritdoc cref="IDatabase.ExecutionSucceeded"/>
    public event ExecutionSucceededEventHandler ExecutionSucceeded;

    /// <inheritdoc cref="IDatabase.ConnectionString"/>
    public string ConnectionString => _ConnectionStringGetter();

    /// <inheritdoc cref="IDatabase.ConnectionStringAsync"/>
    public string ConnectionStringAsync => _ConnectionStringGetter() + ";Asynchronous Processing=True";

    /// <summary>
    /// Get the command timeout.
    /// </summary>
    public TimeSpan CommandTimeout => _CommandTimeoutGetter();

    /// <inheritdoc cref="IDatabase.Name"/>
    public string Name { get; }

    /// <inheritdoc cref="IDatabase.GetUtilizedConnectionString(ApplicationIntent?)"/>
    public string GetUtilizedConnectionString(ApplicationIntent? applicationIntent)
        => GetUtilizedConnectionString(ConnectionString, applicationIntent);

    /// <inheritdoc cref="IDatabase.GetUtilizedConnectionStringAsync(ApplicationIntent?)"/>
    public string GetUtilizedConnectionStringAsync(ApplicationIntent? applicationIntent)
        => GetUtilizedConnectionString(ConnectionStringAsync, applicationIntent);

    /// <summary>
    /// Creates a new instance of <see cref="Database"/>.
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <param name="connectionStringGetter">A function that returns the connection string.</param>
    /// <param name="commandTimeoutGetter">A function that returns the command timeout.</param>
    /// <exception cref="ArgumentException">Invalid value for argument <paramref name="name"/>.</exception>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="connectionStringGetter"/> cannot be null.
    /// - <paramref name="commandTimeoutGetter"/> cannot be null.
    /// </exception>
    public Database(string name, Func<string> connectionStringGetter, Func<TimeSpan> commandTimeoutGetter)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Invalid value for argument name", nameof(name));
        Name = name;

        _ConnectionStringGetter = connectionStringGetter ?? throw new ArgumentNullException(nameof(connectionStringGetter));
        _CommandTimeoutGetter = commandTimeoutGetter ?? throw new ArgumentNullException(nameof(commandTimeoutGetter));
    }

    /// <summary>
    /// Deconstructs the database.
    /// </summary>
    ~Database()
    {
        Dispose(false);
    }


    private void OnExecutionFailed(DatabaseExecutionEventArgs e)
        => ExecutionFailed?.Invoke(this, e);
    private void OnExecutionFinished(DatabaseExecutionEventArgs e)
        => ExecutionFinished?.Invoke(this, e);
    private void OnExecutionStarted(DatabaseExecutionEventArgs e)
        => ExecutionStarted?.Invoke(this, e);
    private void OnExecutionSucceeded(DatabaseExecutionEventArgs e)
        => ExecutionSucceeded?.Invoke(this, e);

    /// <summary>
    /// Execute the SQL command.
    /// </summary>
    /// <param name="commandType">The command type</param>
    /// <param name="commandText">The command text</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="action">The SQL action to execute</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application timeout</param>
    /// <exception cref="ArgumentException">Invalid value for argument <paramref name="commandText"/></exception>
    protected virtual void Execute(CommandType commandType, string commandText, SqlParameter[] sqlParameters, Action<SqlCommand> action, int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            throw new ArgumentException("Invalid value for argument commandText", nameof(commandText));

        OnExecutionStarting();

        var requestId = Interlocked.Increment(ref _RequestCounter);

        var args = new DatabaseExecutionEventArgs(this, commandType, commandText, sqlParameters, requestId);

        OnExecutionStarted(args);

        try
        {
            using var thisConnection = new SqlConnection(GetUtilizedConnectionString(applicationIntent));
            using var thisCommand = new SqlCommand(commandText, thisConnection);

            thisCommand.CommandType = commandType;
            thisCommand.CommandTimeout = commandTimeout ?? _CommandTimeoutInSeconds;

            if (sqlParameters != null)
                thisCommand.Parameters.AddRange(sqlParameters);

            thisConnection.Open();
            action(thisCommand);

            OnExecutionSucceeded(args);
        }
        catch (Exception ex)
        {
            args.Exception = ex;
            OnExecutionFailed(args);

            throw ex;
        }
        finally
        {
            OnExecutionFinished(args);
        }
    }

    /// <summary>
    /// Executes an SQL Command
    /// </summary>
    /// <param name="commandType">The command type</param>
    /// <param name="commandText">The command text</param>
    /// <param name="sqlParameters">The SQL Parameters</param>
    /// <param name="action">The SQL action to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <param name="commandTimeout">The command timeout</param>
    /// <param name="applicationIntent">The application timeout</param>
    /// <exception cref="ArgumentException">Invalid value for argument</exception>
    protected virtual async Task ExecuteAsync(CommandType commandType, string commandText, SqlParameter[] sqlParameters, Func<SqlCommand, CancellationToken, Task> action, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            throw new ArgumentException("Invalid value for argument commandText", nameof(commandText));

        await OnExecutionStartingAsync(cancellationToken);

        var requestId = Interlocked.Increment(ref _RequestCounter);

        var args = new DatabaseExecutionEventArgs(this, commandType, commandText, sqlParameters, requestId);

        OnExecutionStarted(args);

        try
        {
            using var thisConnection = new SqlConnection(GetUtilizedConnectionString(applicationIntent));
            using var thisCommand = new SqlCommand(commandText, thisConnection);

            thisCommand.CommandType = commandType;
            thisCommand.CommandTimeout = commandTimeout ?? _CommandTimeoutInSeconds;

            if (sqlParameters != null)
                thisCommand.Parameters.AddRange(sqlParameters);

            await thisConnection.OpenAsync(cancellationToken);
            await action(thisCommand, cancellationToken);

            OnExecutionSucceeded(args);
        }
        catch (Exception ex)
        {
            args.Exception = ex;
            OnExecutionFailed(args);

            throw ex;
        }
        finally
        {
            OnExecutionFinished(args);
        }
    }

    /// <summary>
    /// Called when execution is starting.
    /// </summary>
    protected virtual void OnExecutionStarting()
    {
    }

    /// <summary>
    /// Called when execution is starting asynchronously.
    /// </summary>
    protected virtual Task OnExecutionStartingAsync(CancellationToken cancellationToken = default(CancellationToken))
        => Task.FromResult(true);

    /// <inheritdoc cref="IDatabase.ExecuteNonQuery(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public int ExecuteNonQuery(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        var numberOfRecordsAffected = 0;

        Execute(
            commandType,
            commandText,
            sqlParameters,
            Action,
            commandTimeout,
            applicationIntent
        );

        return numberOfRecordsAffected;

        void Action(SqlCommand command)
        {
            numberOfRecordsAffected = command.ExecuteNonQuery();
        }
    }

    /// <inheritdoc cref="IDatabase.ExecuteNonQueryAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public async Task<int> ExecuteNonQueryAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        var numberOfRecordsAffected = 0;

        await ExecuteAsync(
            commandType,
            commandText,
            sqlParameters,
            Action,
            cancellationToken,
            commandTimeout,
            applicationIntent
        ).ConfigureAwait(false);

        return numberOfRecordsAffected;

        async Task Action(SqlCommand command, CancellationToken cancellationToken)
        {
            numberOfRecordsAffected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="IDatabase.ExecuteReader(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public IEnumerable<IDictionary<string, object>> ExecuteReader(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        IEnumerable<IDictionary<string, object>> dataReader = null;

        Execute(
            commandType,
            commandText,
            sqlParameters,
            Action,
            commandTimeout,
            applicationIntent
        );

        return dataReader;

        void Action(SqlCommand command)
        {
            dataReader = Read(command);
        }
    }

    /// <inheritdoc cref="IDatabase.ExecuteReaderAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public async Task<IEnumerable<IDictionary<string, object>>> ExecuteReaderAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        IEnumerable<IDictionary<string, object>> dataReader = null;

        await ExecuteAsync(
            commandType,
            commandText,
            sqlParameters,
            Action,
            cancellationToken,
            commandTimeout,
            applicationIntent
        ).ConfigureAwait(false);

        return dataReader;

        async Task Action(SqlCommand command, CancellationToken cancellationToken)
        {
            dataReader = await ReadAsync(command, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="IDatabase.ExecuteScalar(string, SqlParameter[], CommandType, int?, ApplicationIntent?)"/>
    public object ExecuteScalar(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        object result = null;

        Execute(
            commandType,
            commandText,
            sqlParameters,
            Action,
            commandTimeout,
            applicationIntent
        );

        return result;

        void Action(SqlCommand command)
        {
            result = command.ExecuteScalar();
        }
    }

    /// <inheritdoc cref="IDatabase.ExecuteScalarAsync(string, SqlParameter[], CommandType, CancellationToken, int?, ApplicationIntent?)"/>
    public async Task<object> ExecuteScalarAsync(string commandText, SqlParameter[] sqlParameters, CommandType commandType = CommandType.StoredProcedure, CancellationToken cancellationToken = default(CancellationToken), int? commandTimeout = null, ApplicationIntent? applicationIntent = null)
    {
        object result = null;

        await ExecuteAsync(
            commandType,
            commandText,
            sqlParameters,
            Action,
            cancellationToken,
            commandTimeout,
            applicationIntent
        );

        return result;

        async Task Action(SqlCommand command, CancellationToken cancellationToken)
        {
            result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetUtilizedConnectionString(string baseConnectionString, ApplicationIntent? applicationIntent)
    {
        if (applicationIntent == null)
            return baseConnectionString;

        return string.Format(
            "{0}{1}applicationintent={2}",
            baseConnectionString,
            baseConnectionString.EndsWith(";")
                ? string.Empty
                : ";",
            applicationIntent.Value
        );
    }

    private static IEnumerable<IDictionary<string, object>> Read(SqlCommand sqlCommand)
    {
        var records = new List<IDictionary<string, object>>();
        using var reader = sqlCommand.ExecuteReader();

        var fieldNames = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            fieldNames[i] = reader.GetName(i);

        while (reader.Read())
        {
            var fieldValues = new Dictionary<string, object>(reader.FieldCount);
            for (int j = 0; j < reader.FieldCount; j++)
                fieldValues[fieldNames[j]] = reader[j] == DBNull.Value
                    ? null
                    : reader[j];

            records.Add(fieldValues);
        }

        return records;
    }

    private static async Task<IEnumerable<IDictionary<string, object>>> ReadAsync(SqlCommand sqlCommand, CancellationToken cancellationToken = default(CancellationToken))
    {
        var records = new List<IDictionary<string, object>>();
        using var reader = await sqlCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        var fieldNames = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            fieldNames[i] = reader.GetName(i);

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var fieldValues = new Dictionary<string, object>(reader.FieldCount);
            for (int j = 0; j < reader.FieldCount; j++)
            {
                var field = await reader.GetFieldValueAsync<object>(j, cancellationToken).ConfigureAwait(false);

                fieldValues[fieldNames[j]] = field == DBNull.Value
                    ? null
                    : field;
            }

            records.Add(fieldValues);
        }

        return records;
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
    }
}
