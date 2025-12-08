using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace Cooldown.Desktop.IPC;

/// <summary>
/// Named pipe client for IPC per docs/IPC-Contract-v0.1.md.
/// - Transport: Windows named pipe (duplex), newline-delimited JSON envelopes.
/// - Single persistent connection per instance, with background listener for responses and events.
/// </summary>
public sealed class NamedPipeClient : INamedPipeClient
{
    private const string PipeName = "Cooldown.Service.IPC";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<MessageEnvelope>> _pendingResponses = new();

    private NamedPipeClientStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private CancellationTokenSource? _listenCts;
    private Task? _listenTask;

    public event Action<LockStateChangedEventPayload>? LockStateChanged;

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_stream is { IsConnected: true })
        {
            return true;
        }

        CleanupStreams();

        _stream = new NamedPipeClientStream(
            serverName: ".",
            pipeName: PipeName,
            direction: PipeDirection.InOut,
            options: PipeOptions.Asynchronous);

        try
        {
            await _stream.ConnectAsync(cancellationToken).ConfigureAwait(false);
            _reader = new StreamReader(_stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
            _writer = new StreamWriter(_stream, new UTF8Encoding(false)) { AutoFlush = true };
            EnsureListenLoop(cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            CleanupStreams();
            Debug.WriteLine("NamedPipeClient: connect canceled.");
            return false;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException)
        {
            CleanupStreams();
            Debug.WriteLine($"NamedPipeClient: connect failed ({ex.Message}).");
            return false;
        }
    }

    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
        if (connected)
        {
            EnsureListenLoop(cancellationToken);
        }
    }

    public async Task<CommandResponse<TResponse>> SendCommandAsync<TRequest, TResponse>(
        string command,
        TRequest requestPayload,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected || _writer == null)
            {
                return BuildUnavailableResponse<TResponse>("Service not reachable (pipe not connected).");
            }

            EnsureListenLoop();

            var correlationId = Guid.NewGuid().ToString("N");
            var pending = new TaskCompletionSource<MessageEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pendingResponses.TryAdd(correlationId, pending))
            {
                return BuildUnavailableResponse<TResponse>("Failed to register pending response handler.");
            }

            var envelope = new MessageEnvelope
            {
                ProtocolVersion = 1,
                MessageType = "Command",
                Command = command,
                CorrelationId = correlationId,
                TimestampUtc = DateTimeOffset.UtcNow,
                Payload = BuildPayloadElement(requestPayload)
            };

            var serialized = JsonSerializer.Serialize(envelope, _serializerOptions);
            await _writer.WriteLineAsync(serialized).ConfigureAwait(false);

            using var ctr = cancellationToken.Register(() => pending.TrySetCanceled(cancellationToken));
            MessageEnvelope responseEnvelope;
            try
            {
                responseEnvelope = await pending.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return BuildUnavailableResponse<TResponse>("Operation canceled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"NamedPipeClient: waiting for response failed ({ex.Message}).");
                return BuildUnavailableResponse<TResponse>("I/O error during pipe communication.");
            }
            finally
            {
                _pendingResponses.TryRemove(correlationId, out _);
            }

            try
            {
                var commandResponse = JsonSerializer.Deserialize<CommandResponse<TResponse>>(responseEnvelope.Payload.GetRawText(), _serializerOptions);
                if (commandResponse != null)
                {
                    return commandResponse;
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to deserialize response payload: {ex.Message}");
            }

            return new CommandResponse<TResponse>
            {
                Success = false,
                Error = new ErrorPayload
                {
                    Code = "InvalidPayload",
                    Message = "Service response payload could not be parsed."
                }
            };
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("NamedPipeClient: send canceled.");
            return BuildUnavailableResponse<TResponse>("Operation canceled.");
        }
        catch (IOException ex)
        {
            CleanupStreams();
            Debug.WriteLine($"NamedPipeClient: I/O error during send/receive ({ex.Message}).");
            return BuildUnavailableResponse<TResponse>("I/O error during pipe communication.");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        CleanupStreams();
        _sendLock.Dispose();
        await Task.CompletedTask;
    }

    private JsonElement BuildPayloadElement<TRequest>(TRequest requestPayload)
    {
        var payloadObj = requestPayload is null ? new { } : (object)requestPayload;
        var json = JsonSerializer.Serialize(payloadObj, payloadObj.GetType(), _serializerOptions);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private void EnsureListenLoop(CancellationToken cancellationToken = default)
    {
        if (_listenTask != null && !_listenTask.IsCompleted)
        {
            return;
        }

        _listenCts?.Cancel();
        _listenCts?.Dispose();
        _listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (_reader == null)
        {
            return;
        }

        _listenTask = Task.Run(() => ListenAsync(_listenCts.Token), CancellationToken.None);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        var reader = _reader;
        if (reader == null)
        {
            return;
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null)
                {
                    FailAllPending(new IOException("Pipe disconnected."));
                    CleanupStreams();
                    break;
                }

                MessageEnvelope? envelope = null;
                try
                {
                    envelope = JsonSerializer.Deserialize<MessageEnvelope>(line, _serializerOptions);
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"Failed to deserialize incoming envelope: {ex.Message}");
                }

                if (envelope == null)
                {
                    continue;
                }

                if (string.Equals(envelope.MessageType, "Response", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(envelope.CorrelationId) &&
                    _pendingResponses.TryRemove(envelope.CorrelationId, out var pending))
                {
                    pending.TrySetResult(envelope);
                    continue;
                }

                if (string.Equals(envelope.MessageType, "Event", StringComparison.OrdinalIgnoreCase))
                {
                    DispatchEvent(envelope);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"NamedPipeClient listener I/O error: {ex.Message}");
            FailAllPending(ex);
            CleanupStreams();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"NamedPipeClient listener error: {ex.Message}");
            FailAllPending(ex);
            CleanupStreams();
        }
    }

    private void DispatchEvent(MessageEnvelope envelope)
    {
        if (!string.Equals(envelope.Command, "Lock.StateChanged", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<LockStateChangedEventPayload>(envelope.Payload.GetRawText(), _serializerOptions);
            if (payload != null)
            {
                LockStateChanged?.Invoke(payload);
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Failed to parse Lock.StateChanged event: {ex.Message}");
        }
    }

    private void FailAllPending(Exception exception)
    {
        foreach (var kvp in _pendingResponses)
        {
            if (_pendingResponses.TryRemove(kvp.Key, out var pending))
            {
                pending.TrySetException(exception);
            }
        }
    }

    private void CleanupStreams()
    {
        try
        {
            _listenCts?.Cancel();
        }
        catch
        {
            // ignore cancellation errors
        }

        _listenTask = null;
        _listenCts?.Dispose();
        _listenCts = null;

        try
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _stream?.Dispose();
        }
        catch
        {
            // ignore disposal errors
        }
        finally
        {
            _writer = null;
            _reader = null;
            _stream = null;
        }

        FailAllPending(new IOException("Pipe disconnected."));
    }

    private static CommandResponse<TResponse> BuildUnavailableResponse<TResponse>(string message)
    {
        return new CommandResponse<TResponse>
        {
            Success = false,
            Error = new ErrorPayload
            {
                Code = "Unavailable",
                Message = message
            }
        };
    }
}
