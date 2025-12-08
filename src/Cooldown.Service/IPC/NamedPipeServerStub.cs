using Microsoft.Extensions.Logging;

namespace Cooldown.Service.IPC;

public sealed class NamedPipeServerStub : INamedPipeServer
{
    private readonly ILogger<NamedPipeServerStub> _logger;

    public NamedPipeServerStub(ILogger<NamedPipeServerStub> logger)
    {
        _logger = logger;
    }

    public Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Named pipe server stub started. TODO: implement IPC endpoints for desktop app.");
        return Task.CompletedTask;
    }

    public Task PollAsync(CancellationToken cancellationToken)
    {
        _logger.LogTrace("Named pipe server stub poll. TODO: accept/handle commands in Phase 2.2.");
        return Task.CompletedTask;
    }

    public Task BroadcastLockStateAsync(string reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Named pipe server stub would broadcast lock state (reason={Reason}).", reason);
        return Task.CompletedTask;
    }
}
