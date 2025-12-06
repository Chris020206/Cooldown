using Cooldown.Service.Engine;
using Cooldown.Service.IPC;
using Cooldown.Service.Options;
using Cooldown.Service.State;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cooldown.Service.Workers;

public sealed class BlockingServiceWorker : BackgroundService
{
    private readonly ILogger<BlockingServiceWorker> _logger;
    private readonly ILockStateManager _lockStateManager;
    private readonly IBlockingEngine _blockingEngine;
    private readonly INamedPipeServer _namedPipeServer;
    private readonly ServiceOptions _options;

    public BlockingServiceWorker(
        ILogger<BlockingServiceWorker> logger,
        ILockStateManager lockStateManager,
        IBlockingEngine blockingEngine,
        INamedPipeServer namedPipeServer,
        IOptions<ServiceOptions> options)
    {
        _logger = logger;
        _lockStateManager = lockStateManager;
        _blockingEngine = blockingEngine;
        _namedPipeServer = namedPipeServer;

        _options = options.Value;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BlockingServiceWorker starting...");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("BlockingServiceWorker stopping...");
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var heartbeatInterval = TimeSpan.FromSeconds(Math.Max(1, _options.HeartbeatIntervalSeconds));
        _logger.LogInformation("BlockingServiceWorker started (heartbeat = {HeartbeatSeconds}s).", heartbeatInterval.TotalSeconds);

        // TODO: listen for IPC commands from desktop app via named pipes once implemented.
        // Future: respond to IPC to start/cancel locks via ILockStateManager.
        await _namedPipeServer.EnsureStartedAsync(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _blockingEngine.PulseAsync(stoppingToken).ConfigureAwait(false);
                await _namedPipeServer.PollAsync(stoppingToken).ConfigureAwait(false);

                var lockState = await _lockStateManager.GetCurrentLockAsync(stoppingToken).ConfigureAwait(false);
                var isActive = lockState?.IsActive(DateTimeOffset.UtcNow) ?? false;
                _logger.LogDebug("Heartbeat tick at {Timestamp}. Lock active: {LockActive}, Type: {LockType}.", DateTimeOffset.Now, isActive, lockState?.Type);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown; swallow to exit gracefully.
                break;
            }
            catch (Exception ex)
            {
                // Keep the service alive; log and continue.
                _logger.LogError(ex, "Unexpected error in BlockingServiceWorker loop.");
            }

            try
            {
                await Task.Delay(heartbeatInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
