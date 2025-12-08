using Cooldown.Service.State;
using Microsoft.Extensions.Logging;
using Cooldown.Service.IPC;

namespace Cooldown.Service.Engine;

/// <summary>
/// Placeholder engine that will later delegate to Cooldown.Blocker.Core for process monitoring
/// and termination. For now it only logs heartbeat-style activity.
/// </summary>
public sealed class BlockingEngineStub : IBlockingEngine
{
    private readonly ILogger<BlockingEngineStub> _logger;
    private readonly ILockStateManager _lockStateManager;
    private readonly INamedPipeServer _namedPipeServer;

    public BlockingEngineStub(ILogger<BlockingEngineStub> logger, ILockStateManager lockStateManager, INamedPipeServer namedPipeServer)
    {
        _logger = logger;
        _lockStateManager = lockStateManager;
        _namedPipeServer = namedPipeServer;
    }

    public Task PulseAsync(CancellationToken cancellationToken)
    {
        return PulseInternalAsync(cancellationToken);
    }

    private async Task PulseInternalAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var current = await _lockStateManager.GetCurrentLockAsync(cancellationToken).ConfigureAwait(false);

        if (current is null)
        {
            _logger.LogInformation("Pulse: no active lock.");
            return;
        }

        if (!current.IsActive(now))
        {
            _logger.LogInformation("Pulse: lock expired at {ExpiresAt}; clearing.", current.ExpiresAt);
            await _lockStateManager.CancelLockAsync(cancellationToken).ConfigureAwait(false);
            await _namedPipeServer.BroadcastLockStateAsync("Expired", cancellationToken).ConfigureAwait(false);
            return;
        }

        var remaining = current.ExpiresAt - now;
        _logger.LogInformation("Pulse: active {Type} lock, remaining={Remaining}, blockedApps={BlockedApps}.", current.Type, remaining, current.BlockedApps.Count);
    }
}
