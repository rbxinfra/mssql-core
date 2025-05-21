namespace Roblox.Mssql;

using System;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Event args for database execution events.
/// </summary>
public class DatabaseExecutionEventArgs : EventArgs
{
    /// <summary>
    /// Gets the command text.
    /// </summary>
    public string CommandText { get; }

    /// <summary>
    /// Gets the command type.
    /// </summary>
    public CommandType CommandType { get; }

    /// <summary>
    /// Gets the database that executed the command.
    /// </summary>
    public Database Database { get; }

    /// <summary>
    /// Gets or sets the exception if any.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Gets the request ID.
    /// </summary>
    public long RequestId { get; }

    /// <summary>
    /// Gets the query parameters.
    /// </summary>
    public SqlParameter[] SqlParameters { get; }

    /// <summary>
    /// Construct a new instance of <see cref="DatabaseExecutionEventArgs"/>
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="commandType">The command type.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="sqlParameters">The query parameters.</param>
    /// <param name="requestId">The request ID.</param>
    public DatabaseExecutionEventArgs(Database database, CommandType commandType, string commandText, SqlParameter[] sqlParameters, long requestId)
    {
        Database = database;
        CommandType = commandType;
        CommandText = commandText;
        SqlParameters = sqlParameters;
        RequestId = requestId;
    }
}
