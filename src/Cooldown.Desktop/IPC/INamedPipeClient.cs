using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cooldown.Desktop.IPC;

public interface INamedPipeClient : IAsyncDisposable
{
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

    Task<CommandResponse<TResponse>> SendCommandAsync<TRequest, TResponse>(
        string command,
        TRequest requestPayload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the background listener for responses and events.
    /// </summary>
    Task StartListeningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Raised when the service pushes a Lock.StateChanged event.
    /// </summary>
    event Action<LockStateChangedEventPayload>? LockStateChanged;
}
