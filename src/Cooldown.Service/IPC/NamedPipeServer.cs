using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Cooldown.Service.IPC.Protocol;
using Cooldown.Service.State;
using Microsoft.Extensions.Logging;

namespace Cooldown.Service.IPC;

/// <summary>
/// Named pipe server implementation for the v0.1 IPC contract (see docs/IPC-Contract-v0.1.md).
/// - Transport: Windows named pipe, duplex, single client.
/// - Framing: UTF-8 JSON, newline-delimited messages.
/// - Behavior: one client at a time; request/response over a single connection.
/// </summary>
public sealed class NamedPipeServer : INamedPipeServer, IAsyncDisposable
{
    private const string PipeName = "Cooldown.Service.IPC";

    private readonly ILogger<NamedPipeServer> _logger;
    private readonly ILockStateManager _lockStateManager;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly object _sync = new();

    private NamedPipeServerStream? _server;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Task? _clientLoopTask;
    private Task? _acceptTask;
    private DateTimeOffset _serviceStart = DateTimeOffset.UtcNow;

    public NamedPipeServer(ILogger<NamedPipeServer> logger, ILockStateManager lockStateManager)
    {
        _logger = logger;
        _lockStateManager = lockStateManager;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_server != null)
        {
            return;
        }

        lock (_sync)
        {
            if (_server != null)
            {
                return;
            }

            _server = new NamedPipeServerStream(
                PipeName,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                transmissionMode: PipeTransmissionMode.Byte,
                options: PipeOptions.Asynchronous | PipeOptions.WriteThrough);
            _logger.LogInformation("Named pipe server listening on \\.\\pipe\\{PipeName}.", PipeName);
        }

        await BeginAcceptAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PollAsync(CancellationToken cancellationToken)
    {
        await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

        if (_server == null)
        {
            return;
        }

        // If accept task faulted/canceled, reset server to allow a new listener.
        if (_acceptTask != null && _acceptTask.IsCompleted)
        {
            if (_acceptTask.IsFaulted)
            {
                _logger.LogError(_acceptTask.Exception, "Pipe accept task faulted; restarting listener.");
            }

            _acceptTask = null;
            if (_server.IsConnected == false)
            {
                await BeginAcceptAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        // If client loop completed (disconnect/error), cleanup and restart listener.
        if (_clientLoopTask != null && _clientLoopTask.IsCompleted)
        {
            if (_clientLoopTask.IsFaulted)
            {
                _logger.LogError(_clientLoopTask.Exception, "Pipe client loop faulted; resetting connection.");
            }

            await CleanupClientAsync().ConfigureAwait(false);
            await BeginAcceptAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupClientAsync().ConfigureAwait(false);
        _server?.Dispose();
    }

    private Task BeginAcceptAsync(CancellationToken cancellationToken)
    {
        if (_server == null)
        {
            return Task.CompletedTask;
        }

        if (_server.IsConnected)
        {
            return Task.CompletedTask;
        }

        if (_acceptTask == null)
        {
            _acceptTask = WaitForConnectionAsyncSafe(_server, cancellationToken);
        }

        if (_acceptTask.IsCompletedSuccessfully)
        {
            _acceptTask = null;
            InitializeClientStreams();
            _clientLoopTask = RunClientLoopAsync(_server, _reader!, _writer!, cancellationToken);
            _logger.LogInformation("Named pipe client connected.");
        }

        return Task.CompletedTask;
    }

    private Task WaitForConnectionAsyncSafe(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while waiting for pipe connection.");
                throw;
            }
        }, cancellationToken);
    }

    private void InitializeClientStreams()
    {
        if (_server == null)
        {
            return;
        }

        _reader = new StreamReader(_server, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        _writer = new StreamWriter(_server, new UTF8Encoding(false)) { AutoFlush = true };
    }

    private async Task RunClientLoopAsync(NamedPipeServerStream server, StreamReader reader, StreamWriter writer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && server.IsConnected)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    _logger.LogInformation("Named pipe client disconnected.");
                    break;
                }

                MessageEnvelope? envelope = null;
                try
                {
                    envelope = JsonSerializer.Deserialize<MessageEnvelope>(line, _serializerOptions);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse incoming IPC message: {Line}", line);
                    continue;
                }

                if (envelope == null)
                {
                    continue;
                }

                await HandleEnvelopeAsync(envelope, writer, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Named pipe IO error; client loop ending.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in pipe client loop.");
        }
        finally
        {
            await CleanupClientAsync().ConfigureAwait(false);
        }
    }

    private async Task HandleEnvelopeAsync(MessageEnvelope envelope, StreamWriter writer, CancellationToken cancellationToken)
    {
        if (!string.Equals(envelope.MessageType, "Command", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Ignoring non-command message type: {MessageType}", envelope.MessageType);
            return;
        }

        var responsePayload = await DispatchCommandAsync(envelope, cancellationToken).ConfigureAwait(false);

        var response = new MessageEnvelope
        {
            ProtocolVersion = envelope.ProtocolVersion,
            MessageType = "Response",
            Command = envelope.Command,
            CorrelationId = envelope.CorrelationId,
            TimestampUtc = DateTimeOffset.UtcNow,
            Payload = JsonSerializer.SerializeToElement(responsePayload, _serializerOptions)
        };

        var json = JsonSerializer.Serialize(response, _serializerOptions);
        await writer.WriteLineAsync(json).ConfigureAwait(false);
    }

    private async Task<object> DispatchCommandAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        return envelope.Command switch
        {
            "Service.Ping" => BuildPingResponse(),
            "Lock.GetState" => await HandleLockGetStateAsync(cancellationToken).ConfigureAwait(false),
            "Lock.Cancel" => await HandleLockCancelAsync(cancellationToken).ConfigureAwait(false),
            // TODO: implement Lock.Create and Apps.* commands in later steps.
            _ => CommandResponse.FromError("NotImplemented", "Command not implemented in this phase.")
        };
    }

    private object BuildPingResponse()
    {
        var uptimeSeconds = (int)(DateTimeOffset.UtcNow - _serviceStart).TotalSeconds;
        return new CommandResponse
        {
            Success = true,
            Result = new
            {
                serviceVersion = GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0",
                uptimeSeconds,
                protocolVersion = 1
            }
        };
    }

    private async Task<object> HandleLockGetStateAsync(CancellationToken cancellationToken)
    {
        var current = await _lockStateManager.GetCurrentLockAsync(cancellationToken).ConfigureAwait(false);
        if (current == null)
        {
            return new CommandResponse
            {
                Success = true,
                Result = new { hasActiveLock = false }
            };
        }

        var now = DateTimeOffset.UtcNow;
        var remaining = current.ExpiresAt - now;
        if (remaining < TimeSpan.Zero)
        {
            remaining = TimeSpan.Zero;
        }

        return new CommandResponse
        {
            Success = true,
            Result = new
            {
                hasActiveLock = true,
                @lock = new
                {
                    lockId = current.Id,
                    type = current.Type.ToString(),
                    startedAtUtc = current.StartedAt,
                    expiresAtUtc = current.ExpiresAt,
                    durationSeconds = (int)current.Duration.TotalSeconds,
                    remainingSeconds = (int)remaining.TotalSeconds,
                    blockedApps = current.BlockedApps
                }
            }
        };
    }

    private async Task<object> HandleLockCancelAsync(CancellationToken cancellationToken)
    {
        var current = await _lockStateManager.GetCurrentLockAsync(cancellationToken).ConfigureAwait(false);
        if (current == null)
        {
            return new CommandResponse
            {
                Success = true,
                Result = new { canceled = false, reason = "NoActiveLock" }
            };
        }

        await _lockStateManager.CancelLockAsync(cancellationToken).ConfigureAwait(false);
        return new CommandResponse
        {
            Success = true,
            Result = new { canceled = true, previousLockId = current.Id }
        };
    }

    private async Task CleanupClientAsync()
    {
        try
        {
            if (_clientLoopTask != null && !_clientLoopTask.IsCompleted)
            {
                await _clientLoopTask.ConfigureAwait(false);
            }
        }
        catch
        {
            // Swallow; we are tearing down.
        }

        if (_writer != null)
        {
            await _writer.FlushAsync().ConfigureAwait(false);
            _writer.Dispose();
        }

        _reader?.Dispose();

        if (_server != null)
        {
            try
            {
                if (_server.IsConnected)
                {
                    _server.Disconnect();
                }
            }
            catch
            {
                // ignore
            }
        }

        _writer = null;
        _reader = null;
        _clientLoopTask = null;
    }
}
