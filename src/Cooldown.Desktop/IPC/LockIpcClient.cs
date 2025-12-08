using System.Threading;
using System.Threading.Tasks;

namespace Cooldown.Desktop.IPC;

public sealed class LockIpcClient : ILockIpcClient
{
    private readonly INamedPipeClient _pipeClient;

    public LockIpcClient(INamedPipeClient pipeClient)
    {
        _pipeClient = pipeClient;
    }

    public event Action<LockStateChangedEventPayload>? LockStateChanged
    {
        add => _pipeClient.LockStateChanged += value;
        remove => _pipeClient.LockStateChanged -= value;
    }

    public Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        return _pipeClient.StartListeningAsync(cancellationToken);
    }

    public Task<CommandResponse<LockCreateResponse>> CreateLockAsync(LockCreateRequest request, CancellationToken cancellationToken = default)
    {
        return _pipeClient.SendCommandAsync<LockCreateRequest, LockCreateResponse>("Lock.Create", request, cancellationToken);
    }

    public Task<CommandResponse<LockCancelResponse>> CancelLockAsync(LockCancelRequest request, CancellationToken cancellationToken = default)
    {
        return _pipeClient.SendCommandAsync<LockCancelRequest, LockCancelResponse>("Lock.Cancel", request, cancellationToken);
    }

    public Task<CommandResponse<LockStateResponse>> GetLockStateAsync(CancellationToken cancellationToken = default)
    {
        return _pipeClient.SendCommandAsync<object, LockStateResponse>("Lock.GetState", new { }, cancellationToken);
    }
}
