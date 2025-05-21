namespace Roblox.Mssql;

using System.Collections.Concurrent;

using Instrumentation;

internal class DatabaseMonitor
{
    private readonly ICounterRegistry _CounterRegistry;
    private readonly string _Category;
    private readonly ConcurrentDictionary<string, ExecutionCounterSet> _ActionMonitors = new();
    private readonly ExecutionCounterSet _TotalCounterSet;

    private const int _MaximumInstanceNameLength = 127;
    private const string _AverageResponseTimeCounterName = "Avg Response Time";
    private const string _FailuresPerSecondCounterName = "Failures/s";
    private const string _RequestsOutstandingCounterName = "Requests Outstanding";
    private const string _RequestsPerSecondCounterName = "Requests/s";
    private const string _TotalInstanceName = "_Total";
    private const string _DatabaseCategoryPrefix = "Roblox.Mssql.";

    internal DatabaseMonitor(string databaseName, ICounterRegistry counterRegistry)
    {
        _CounterRegistry = counterRegistry;
        _Category = _DatabaseCategoryPrefix + databaseName;
        _TotalCounterSet = GetOrCreateAction(_TotalInstanceName);
    }

    internal IAverageValueCounter GetAverageResponseTime() => _TotalCounterSet.AverageResponseTime;
    internal IRateOfCountsPerSecondCounter GetFailuresPerSecond() => _TotalCounterSet.FailuresPerSecond;
    internal IRawValueCounter GetRequestsOutstanding() => _TotalCounterSet.RequestsOutstanding;
    internal IRateOfCountsPerSecondCounter GetRequestsPerSecond() => _TotalCounterSet.RequestsPerSecond;

    internal ExecutionCounterSet GetOrCreateAction(string actionName)
    {
        if (actionName.Length > _MaximumInstanceNameLength)
            actionName = actionName.Substring(0, _MaximumInstanceNameLength);

        return _ActionMonitors.GetOrAdd(actionName,CreateActionMonitor);
    }

    private ExecutionCounterSet CreateActionMonitor(string actionName)
    {
        return new ExecutionCounterSet(
            _CounterRegistry.GetRateOfCountsPerSecondCounter(_Category, _RequestsPerSecondCounterName, actionName), 
            _CounterRegistry.GetRateOfCountsPerSecondCounter(_Category, _FailuresPerSecondCounterName, actionName), 
            _CounterRegistry.GetRawValueCounter(_Category, _RequestsOutstandingCounterName, actionName), 
            _CounterRegistry.GetAverageValueCounter(_Category, _AverageResponseTimeCounterName, actionName)
        );
    }
}
