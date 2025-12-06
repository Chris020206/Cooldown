using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cooldown.Desktop.IPC;

/// <summary>
/// Named pipe client for IPC per docs/IPC-Contract-v0.1.md.
/// - Transport: Windows named pipe (duplex), newline-delimited JSON envelopes.
/// - Single persistent connection per instance.
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
    private NamedPipeClientStream? _stream;
    private StreamReader? _reader;
    private StreamWriter? _writer;

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

    public async Task<CommandResponse<TResponse>> SendCommandAsync<TRequest, TResponse>(
        string command,
        TRequest requestPayload,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connected = await ConnectAsync(cancellationToken).ConfigureAwait(false);
            if (!connected || _writer == null || _reader == null)
            {
                return new CommandResponse<TResponse>
                {
                    Success = false,
                    Error = new ErrorPayload
                    {
                        Code = "Unavailable",
                        Message = "Service not reachable (pipe not connected)."
                    }
                };
            }

            var envelope = new MessageEnvelope
            {
                ProtocolVersion = 1,
                MessageType = "Command",
                Command = command,
                CorrelationId = Guid.NewGuid().ToString("N"),
                TimestampUtc = DateTimeOffset.UtcNow,
                Payload = BuildPayloadElement(requestPayload)
            };

            var serialized = JsonSerializer.Serialize(envelope, _serializerOptions);
            await _writer.WriteLineAsync(serialized).ConfigureAwait(false);

            var line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null)
            {
                CleanupStreams();
                return new CommandResponse<TResponse>
                {
                    Success = false,
                    Error = new ErrorPayload
                    {
                        Code = "Disconnected",
                        Message = "Pipe disconnected while waiting for response."
                    }
                };
            }

            MessageEnvelope? responseEnvelope = null;
            try
            {
                responseEnvelope = JsonSerializer.Deserialize<MessageEnvelope>(line, _serializerOptions);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to deserialize response envelope: {ex.Message}");
                return new CommandResponse<TResponse>
                {
                    Success = false,
                    Error = new ErrorPayload
                    {
                        Code = "ParseError",
                        Message = "Invalid response envelope from service."
                    }
                };
            }

            if (responseEnvelope == null)
            {
                return new CommandResponse<TResponse>
                {
                    Success = false,
                    Error = new ErrorPayload
                    {
                        Code = "EmptyResponse",
                        Message = "Service returned an empty response."
                    }
                };
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
            return new CommandResponse<TResponse>
            {
                Success = false,
                Error = new ErrorPayload
                {
                    Code = "Canceled",
                    Message = "Operation canceled."
                }
            };
        }
        catch (IOException ex)
        {
            CleanupStreams();
            Debug.WriteLine($"NamedPipeClient: I/O error during send/receive ({ex.Message}).");
            return new CommandResponse<TResponse>
            {
                Success = false,
                Error = new ErrorPayload
                {
                    Code = "IOError",
                    Message = "I/O error during pipe communication."
                }
            };
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

    private void CleanupStreams()
    {
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
    }
}
