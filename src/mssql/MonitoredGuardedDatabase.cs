namespace Roblox.Mssql;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// A guarded database that is monitored by perfmon.
/// </summary>
public class MonitoredGuardedDatabase : GuardedDatabase
{
    private readonly Func<IDatabase, IDatabaseObserver> _DatabaseObserverBuilder;

    private bool _IsDisposed;
    private bool _IsMonitoring;
    private IDatabaseObserver _DatabaseObserver;
    private readonly SemaphoreSlim _Semaphore = new(1, 1);

    /// <summary>
    /// Construct a new instance of <see cref="MonitoredGuardedDatabase"/>
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <param name="connectionStringGetter">The connection string getter.</param>
    /// <param name="commandTimeoutGetter">The command timeout getter.</param>
    /// /// <param name="retryInterval">The retry interval for the circuit breaker.</param>
    /// <param name="databaseObserverBuilder">The database observer getter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="databaseObserverBuilder"/> cannot be null.</exception>
    public MonitoredGuardedDatabase(string name, Func<string> connectionStringGetter, Func<TimeSpan> commandTimeoutGetter, TimeSpan retryInterval, Func<IDatabase, IDatabaseObserver> databaseObserverBuilder)
        : base(name, connectionStringGetter, commandTimeoutGetter, retryInterval)
    {
        _DatabaseObserverBuilder = databaseObserverBuilder ?? throw new ArgumentNullException(nameof(databaseObserverBuilder));
    }

    /// <inheritdoc cref="Database.Dispose(bool)"/>
    protected override void Dispose(bool disposing)
    {
        if (_IsDisposed) return;

        if (disposing)
        {
            StopMonitoring();
            _Semaphore?.Dispose();
        }

        _IsDisposed = true;
    }

    /// <inheritdoc cref="Database.OnExecutionStarting"/>
    protected override void OnExecutionStarting() => StartMonitoring();

    /// <inheritdoc cref="Database.OnExecutionStartingAsync(CancellationToken)"/>
    protected override Task OnExecutionStartingAsync(CancellationToken cancellationToken = default(CancellationToken))
        => StartMonitoringAsync(cancellationToken);

    /// <summary>
    /// Begin monitoring.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The class is disposed.</exception>
    public void StartMonitoring()
    {
        if (_IsDisposed) throw new ObjectDisposedException(GetType().ToString());
        if (_IsMonitoring) return;

        try
        {
            _Semaphore.Wait();
            if (_IsMonitoring) return;

            _IsMonitoring = true;
        }
        finally
        {
            _Semaphore.Release();
        }

        _DatabaseObserver = _DatabaseObserverBuilder(this);
        _DatabaseObserver.Register();
    }

    /// <summary>
    /// Start monitoring asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="ObjectDisposedException">The class is disposed.</exception>
    public async Task StartMonitoringAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (_IsDisposed) throw new ObjectDisposedException(GetType().ToString());
        if (_IsMonitoring) return;

        try
        {
            await _Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (_IsMonitoring) return;

            _IsMonitoring = true;
        }
        finally
        {
            _Semaphore.Release();
        }

        _DatabaseObserver = _DatabaseObserverBuilder(this);
        _DatabaseObserver.Register();
    }

    /// <summary>
    /// Stop monitoring.
    /// </summary>
    public void StopMonitoring()
    {
        if (!_IsMonitoring) return;

        try
        {
            _Semaphore.Wait();
            if (!_IsMonitoring) return;

            _IsMonitoring = false;
        }
        finally
        {
            _Semaphore.Release();
        }

        _DatabaseObserver.Dispose();
        _DatabaseObserver = null;
    }

    /// <summary>
    /// Stop monitoring asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>An awaitable task.</returns>
    /// <exception cref="ObjectDisposedException">The class is disposed.</exception>
    public async Task StopMonitoringAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (!_IsMonitoring) return;

        try
        {
            await _Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!_IsMonitoring) return;

            _IsMonitoring = false;
        }
        finally
        {
            _Semaphore.Release();
        }

        _DatabaseObserver.Dispose();
        _DatabaseObserver = null;
    }
}
