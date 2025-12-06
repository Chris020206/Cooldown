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
}
