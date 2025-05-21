namespace Roblox.Mssql;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Interface for a database action observer.
/// </summary>
public interface IDatabaseObserver : IDisposable
{
    /// <summary>
    /// Register the observer.
    /// </summary>
    void Register();

    /// <summary>
    /// Register the observer asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The awaitable task.</returns>
    Task RegisterAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Unregister the observer.
    /// </summary>
    void Unregister();

    /// <summary>
    /// Unregister the observer asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The awaitable task.</returns>
    Task UnregisterAsync(CancellationToken cancellationToken = default(CancellationToken));
}
