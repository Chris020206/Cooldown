using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cooldown.Desktop.IPC;

public interface ILockIpcClient
{
    Task<CommandResponse<LockCreateResponse>> CreateLockAsync(LockCreateRequest request, CancellationToken cancellationToken = default);

    Task<CommandResponse<LockCancelResponse>> CancelLockAsync(LockCancelRequest request, CancellationToken cancellationToken = default);

    Task<CommandResponse<LockStateResponse>> GetLockStateAsync(CancellationToken cancellationToken = default);

    Task StartListeningAsync(CancellationToken cancellationToken = default);

    event Action<LockStateChangedEventPayload>? LockStateChanged;
}
