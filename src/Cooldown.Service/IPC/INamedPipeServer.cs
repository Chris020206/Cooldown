namespace Cooldown.Service.IPC;

public interface INamedPipeServer
{
    /// <summary>
    /// Starts the named pipe listener if not already running. Safe to call multiple times.
    /// </summary>
    Task EnsureStartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs a unit of work for accepting/servicing a client connection. Should return regularly.
    /// </summary>
    Task PollAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Broadcasts the current lock state to the connected client (if any) as an Event message.
    /// Safe to call even when no client is connected.
    /// </summary>
    Task BroadcastLockStateAsync(string reason, CancellationToken cancellationToken);
}
