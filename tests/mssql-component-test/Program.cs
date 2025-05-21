using System.Data;
using System.Text.Json;
using System.Diagnostics;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

using Prometheus;
using Roblox.Mssql;
using Roblox.Networking;
using Roblox.Instrumentation;

var precheckFailureCounter = Metrics.CreateCounter("precheck_failure", "Number of times the precheck failed");
var executionCounter = Metrics.CreateCounter("sql_execution_count", "Number of SQL executions", "database", "query");
var executionDuration = Metrics.CreateHistogram("sql_execution_duration", "Duration of SQL executions", "database", "query");
var executionError = Metrics.CreateCounter("sql_execution_error", "Number of SQL executions with errors", "database", "query");

var instrumentationPrecheckFailureCounter = StaticCounterRegistry.Instance.GetRawValueCounter("Roblox.Mssql.Component", "PrecheckFailures");
var instrumentationExecutionCounter = StaticCounterRegistry.Instance.GetRawValueCounter("Roblox.Mssql.Component", "Executions");
var instrumentationExecutionDuration = StaticCounterRegistry.Instance.GetAverageValueCounter("Roblox.Mssql.Component", "ExecutionDuration");
var instrumentationExecutionError = StaticCounterRegistry.Instance.GetRawValueCounter("Roblox.Mssql.Component", "ExecutionErrors");

Environment.SetEnvironmentVariable("LogLevel", "Trace");
Environment.SetEnvironmentVariable("AppName", "Roblox.Mssql.Component.Test");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddMetrics();

var app = builder.Build();

string? defaultConnectionString = app.Configuration.GetConnectionString("DefaultConnection");
var allowedRanges = IpAddressRange.ParseStringList(app.Configuration.GetValue<string>("AllowedCIDRs"));

GuardedDatabase? db = null;

app.Use(async (ctx, next) =>
{
    var ip = ctx.Connection.RemoteIpAddress?.ToString();

    ctx.Response.Headers.Append("Roblox-Machine-Id", Environment.MachineName);
    ctx.Response.Headers.Append("Roblox-Application-Name", Environment.GetEnvironmentVariable("AppName"));

    Console.WriteLine("Request from {0}", ip);

    if (allowedRanges?.Count != 0)
    {

        if (ip == null || !IPAddressUtils.IsIpAddressAllowed(ctx.Connection.RemoteIpAddress, allowedRanges))
        {
            ctx.Response.StatusCode = 403;

            await ctx.Response.WriteAsJsonAsync("IP address is not allowed.");

            return;
        }
    }

    await next();
});

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        if (app.Environment.IsDevelopment())
        {
            ctx.Response.ContentType = "text/plain";

            await ctx.Response.WriteAsync(ex.ToString());
        }
    }
});

app.UseRouting();

app.UseMetricServer("/metrics");
app.UseHealthChecks("/health");

app.MapFallback(async ctx =>
{
    ctx.Response.ContentType = "text/plain";
    ctx.Response.Headers.Append("X-Allowed-Paths", "/, /health, /metrics, /query");
    ctx.Response.StatusCode = 404;

    await ctx.Response.WriteAsync("Valid paths are /, /health, /metrics and /query.");
});

app.MapGet("/query", async ctx =>
{
    // If there are no query, or the 'help' query key is specified, then show the help text.
    if (ctx.Request.Query.Count == 0 || ctx.Request.Query.ContainsKey("help"))
    {
        ctx.Response.ContentType = "text/plain";
        ctx.Response.StatusCode = 200;
        await ctx.Response.WriteAsync(@$"Roblox MS SQL Component Test.

Valid query string:
[...]: Arguments to be passed into the SQL Query. If the query name doesn't start with @, it will be prepended.
[q]: The SQL query to execute.
[connectionString]: A connection string to use. Defaults to connection string set in config.
[catalog]: The database to connect to.
[help]: Displays this help text.
");
        return;
    }


    var connectionString = ctx.Request.Query["connectionString"].FirstOrDefault();

    if (!string.IsNullOrEmpty(connectionString))
    {

        try
        {
            new DbConnectionStringBuilder().ConnectionString = connectionString;
        }
        catch
        {

            precheckFailureCounter.Inc();
            instrumentationPrecheckFailureCounter.Increment();

            // Failed
            const string message = "The argument [connectionString] is invalid.";

            ctx.Response.StatusCode = 400;

            await ctx.Response.WriteAsJsonAsync(message);

            return;
        }
    }
    else
    {
        connectionString = defaultConnectionString;
    }

    var sqlQuery = ctx.Request.Query["q"].FirstOrDefault();

    if (string.IsNullOrEmpty(sqlQuery))
    {
        precheckFailureCounter.Inc();
        instrumentationPrecheckFailureCounter.Increment();

        // Failed
        const string message = "The argument [q] is required.";

        ctx.Response.StatusCode = 400;

        await ctx.Response.WriteAsJsonAsync(message);

        return;
    }

    sqlQuery = sqlQuery.Trim();

    var sqlCatalog = ctx.Request.Query["catalog"].FirstOrDefault();
    if (!string.IsNullOrEmpty(sqlCatalog))
    {
        var cs = new SqlConnectionStringBuilder(connectionString);
        cs.InitialCatalog = sqlCatalog;

        connectionString = cs.ToString();
    }

    if (db == null || !db.ConnectionString.Equals(connectionString, StringComparison.CurrentCultureIgnoreCase))
        db = new("Test", () => connectionString, () => TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));

    // Cut out connectionString and q from query
    var querys = (from q in ctx.Request.Query where !new[] { "q", "help", "catalog", "connectionString" }.Contains(q.Key) select q).ToDictionary(x =>
    {
        var key = x.Key;

        if (!key.StartsWith('@'))
            key = "@" + key;

        return key;
    }, x => x.Value.FirstOrDefault());

    var parameters = new List<SqlParameter>();

    if (querys.Count != 0)
    {
        foreach (var q in querys)
        {
            if (q.Value == null)
                continue;

            // If q.Value is a numeric string, then cast it to either int or double.
            if (Regex.IsMatch(q.Value, @"^\d+$"))
            {
                if (int.TryParse(q.Value, out int i))
                    parameters.Add(new SqlParameter(q.Key, i));
                else if (double.TryParse(q.Value, out double d))
                    parameters.Add(new SqlParameter(q.Key, d));
                else
                    parameters.Add(new SqlParameter(q.Key, q.Value));
            }
            else
            {
                parameters.Add(new SqlParameter(q.Key, q.Value));
            }
        }
    }

    // SQL Server will interpret it as a StoredProcedure call if it has no spaces.
    var commandType = Regex.IsMatch(sqlQuery, @"\s")
        ? CommandType.Text
        : CommandType.StoredProcedure;

    var builder = new SqlConnectionStringBuilder(db.ConnectionString);

    executionCounter.WithLabels(builder.InitialCatalog, sqlQuery).Inc();
    instrumentationExecutionCounter.Increment();

    var sw = Stopwatch.StartNew();

    try
    {


        var record = await db.ExecuteReaderAsync(sqlQuery, parameters.ToArray(), commandType);

        await ctx.Response.WriteAsJsonAsync(
            new { data = record },
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );
    }
    catch (SqlException ex)
    {
        executionError.WithLabels(builder.InitialCatalog, sqlQuery).Inc();
        instrumentationExecutionError.Increment();

        // Failed
        ctx.Response.StatusCode = 500;

        var veryVerboseErrors = app.Configuration.GetValue<bool>("VeryVerboseErrors");

        if (veryVerboseErrors)
            await ctx.Response.WriteAsync(ex.ToString());
        else
            await ctx.Response.WriteAsJsonAsync(ex.Message);
    }
    catch
    {
        executionError.WithLabels(builder.InitialCatalog, sqlQuery).Inc();
        instrumentationExecutionError.Increment();
        throw;
    }
    finally
    {
        sw.Stop();

        executionDuration.Observe(sw.ElapsedMilliseconds);
        instrumentationExecutionDuration.Sample(sw.ElapsedMilliseconds);
    }
});

app.Run();
