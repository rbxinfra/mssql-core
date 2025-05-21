namespace Roblox.Mssql;

/// <summary>
/// Executed when a database action fails.
/// </summary>
/// <param name="sender">The <see cref="Database"/></param>
/// <param name="e">The event args.</param>
public delegate void ExecutionFailedEventHandler(object sender, DatabaseExecutionEventArgs e);

/// <summary>
/// Executed when a database action finishes.
/// </summary>
/// <param name="sender">The <see cref="Database"/></param>
/// <param name="e">The event args.</param>
public delegate void ExecutionFinishedEventHandler(object sender, DatabaseExecutionEventArgs e);

/// <summary>
/// Executed when a database action starts.
/// </summary>
/// <param name="sender">The <see cref="Database"/></param>
/// <param name="e">The event args.</param>
public delegate void ExecutionStartedEventHandler(object sender, DatabaseExecutionEventArgs e);

/// <summary>
/// Executed when a database action succeeds.
/// </summary>
/// <param name="sender">The <see cref="Database"/></param>
/// <param name="e">The event args.</param>
public delegate void ExecutionSucceededEventHandler(object sender, DatabaseExecutionEventArgs e);
