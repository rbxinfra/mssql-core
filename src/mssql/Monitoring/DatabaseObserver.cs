namespace Roblox.Mssql;

using System;
using System.Data;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Instrumentation;

/// <summary>
/// <see cref="IDatabaseObserver"/> implementation.
/// </summary>
public class DatabaseObserver : IDisposable, IDatabaseObserver
{
    private readonly IDatabase _Database;
    private readonly ICounterRegistry _CounterRegistry;
    private DatabaseMonitor _DatabaseMonitor;

    private bool _IsDisposed;
    private bool _IsRegistered;
    private ConcurrentDictionary<long, Stopwatch> _RequestTimers;
    private SemaphoreSlim _Semaphore = new(1, 1);

    /// <summary>
    /// Construct a new instance of <see cref="DatabaseObserver"/>
    /// </summary>
    /// <param name="database">The database being observed.</param>
    /// <param name="counterRegistry">The instrumentation counter registry.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="database"/> cannot be null.
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// </exception>
    public DatabaseObserver(IDatabase database, ICounterRegistry counterRegistry)
    {
        _Database = database ?? throw new ArgumentNullException(nameof(database));
        _CounterRegistry = counterRegistry ?? throw new ArgumentNullException(nameof(counterRegistry));
    }

    /// <summary>
    /// Destructor for <see cref="DatabaseObserver"/>
    /// </summary>
    ~DatabaseObserver()
    {
        Dispose(false);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    protected virtual void Dispose(bool disposing)
    {
        if (_IsDisposed) return;

        if (disposing)
        {
            Unregister();
            _Semaphore?.Dispose();
        }

        _IsDisposed = true;
    }

    /// <inheritdoc cref="IDatabaseObserver.Register"/>
    public void Register()
    {
        if (_IsDisposed) throw new ObjectDisposedException(GetType().ToString());
        if (_IsRegistered) return;

        try
        {
            _Semaphore.Wait();
            if (_IsRegistered) return;

            _IsRegistered = true;
        }
        finally
        {
            _Semaphore.Release();
        }

        DoRegister();
    }

    /// <inheritdoc cref="IDatabaseObserver.RegisterAsync(CancellationToken)"/>
    public async Task RegisterAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_IsDisposed) throw new ObjectDisposedException(GetType().ToString());
        if (_IsRegistered) return;

        try
        {
            await _Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (_IsRegistered)  return;
            
            _IsRegistered = true;
        }
        finally
        {
            _Semaphore.Release();
        }

        DoRegister();
    }

    /// <inheritdoc cref="IDatabaseObserver.Unregister"/>
    public void Unregister()
    {
        if (!_IsRegistered) return;

        try
        {
            _Semaphore.Wait();
            if (!_IsRegistered) return;

            _IsRegistered = false;
        }
        finally
        {
            _Semaphore.Release();
        }

        DoUnregister();
    }

    /// <inheritdoc cref="IDatabaseObserver.UnregisterAsync(CancellationToken)"/>
    public async Task UnregisterAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!_IsRegistered) return;

        try
        {
            await _Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!_IsRegistered) return;

            _IsRegistered = false;
        }
        finally
        {
            _Semaphore.Release();
        }

        DoUnregister();
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void RequestStarted(object sender, DatabaseExecutionEventArgs e)
    {
        var sw = Stopwatch.StartNew();
        _RequestTimers.GetOrAdd(e.RequestId, sw);
        _DatabaseMonitor.GetRequestsOutstanding().Increment();

        if (e.CommandType == CommandType.StoredProcedure)
            _DatabaseMonitor.GetOrCreateAction(e.CommandText).RequestsOutstanding.Increment();
    }

    private void RequestSucceeded(object sender, DatabaseExecutionEventArgs e)
    {
        _DatabaseMonitor.GetRequestsPerSecond().Increment();

        if (e.CommandType == CommandType.StoredProcedure)
            _DatabaseMonitor.GetOrCreateAction(e.CommandText).RequestsPerSecond.Increment();
    }

    private void DoRegister()
    {
        _RequestTimers = new();
        _DatabaseMonitor = new(_Database.Name, _CounterRegistry);

        _Database.ExecutionStarted += RequestStarted;
        _Database.ExecutionSucceeded += RequestSucceeded;
        _Database.ExecutionFailed += RequestFailed;
        _Database.ExecutionFinished += RequestFinished;
    }

    private void DoUnregister()
    {
        _Database.ExecutionStarted -= RequestStarted;
        _Database.ExecutionSucceeded -= RequestSucceeded;
        _Database.ExecutionFailed -= RequestFailed;
        _Database.ExecutionFinished -= RequestFinished;

        _RequestTimers = null;
        _DatabaseMonitor = null;
    }

    private void RequestFailed(object sender, DatabaseExecutionEventArgs e)
    {
        _DatabaseMonitor.GetFailuresPerSecond().Increment();

        if (e.CommandType == CommandType.StoredProcedure)
            _DatabaseMonitor.GetOrCreateAction(e.CommandText).FailuresPerSecond.Increment();
    }

    private void RequestFinished(object sender, DatabaseExecutionEventArgs e)
    {
        if (!_RequestTimers.TryRemove(e.RequestId, out var sw))
            return;

        sw.Stop();

        _DatabaseMonitor.GetAverageResponseTime().Sample(sw.Elapsed.TotalMilliseconds);
        _DatabaseMonitor.GetRequestsOutstanding().Decrement();

        if (e.CommandType == CommandType.StoredProcedure)
        {
            var counters = _DatabaseMonitor.GetOrCreateAction(e.CommandText);
            counters.AverageResponseTime.Sample(sw.Elapsed.TotalMilliseconds);
            counters.RequestsOutstanding.Decrement();
        }
    }
}
