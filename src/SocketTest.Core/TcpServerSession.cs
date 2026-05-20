using System.Net;
using System.Net.Sockets;

namespace SocketTest.Core;

public sealed class TcpServerSession : IAsyncDisposable
{
    public event Action<SessionEvent>? EventRaised;

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _acceptLoop;
    private Task? _readLoop;

    public bool IsListening => _listener is not null;
    public bool HasClient => _client?.Connected == true;

    public PayloadEncoding Encoding { get; set; } = PayloadEncoding.Utf8;
    public LineEnding LineEnding { get; set; } = LineEnding.CrLf;

    public Task StartAsync(string host, int port)
    {
        if (IsListening)
            throw new InvalidOperationException("Already listening.");

        var address = host.Equals("0.0.0.0", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(host)
            ? IPAddress.Any
            : IPAddress.TryParse(host, out var parsed) ? parsed : IPAddress.Any;

        _listener = new TcpListener(address, port);
        _listener.Start();
        _cts = new CancellationTokenSource();
        _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        Raise(SessionEvent.Info($"Listening on {address}:{port}..."));
        return Task.CompletedTask;
    }

    public async Task SendAsync(string text, CancellationToken ct = default)
    {
        if (_stream is null || !HasClient)
            throw new InvalidOperationException("No client connected.");

        var withEnding = LineEnding.AppendLineEnding(text);
        var bytes = Encoding.Encode(withEnding);
        await _stream.WriteAsync(bytes, ct).ConfigureAwait(false);
        await _stream.FlushAsync(ct).ConfigureAwait(false);
        Raise(SessionEvent.Sent(Encoding == PayloadEncoding.Hex
            ? Encoding.Decode(bytes)
            : withEnding));
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        try
        {
            _listener?.Stop();
        }
        catch
        {
        }

        try { if (_readLoop is not null) await _readLoop.ConfigureAwait(false); } catch { }
        try { if (_acceptLoop is not null) await _acceptLoop.ConfigureAwait(false); } catch { }

        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        _listener = null;
        _cts?.Dispose();
        _cts = null;
        _readLoop = null;
        _acceptLoop = null;

        Raise(SessionEvent.Info("Server stopped."));
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        if (_listener is null) return;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                if (_client?.Connected == true)
                {
                    Raise(SessionEvent.Info($"Rejected extra client {client.Client.RemoteEndPoint}."));
                    client.Dispose();
                    continue;
                }
                _client = client;
                _stream = client.GetStream();
                Raise(SessionEvent.Info($"Client connected: {client.Client.RemoteEndPoint}"));
                _readLoop = Task.Run(() => ReadLoopAsync(ct));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Raise(SessionEvent.Error($"Accept error: {ex.Message}"));
        }
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_stream is null) return;
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await _stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                if (read <= 0)
                {
                    Raise(SessionEvent.Info("Client disconnected."));
                    _stream.Dispose();
                    _client?.Dispose();
                    _stream = null;
                    _client = null;
                    return;
                }
                Raise(SessionEvent.Received(Encoding.Decode(buffer.AsSpan(0, read))));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            Raise(SessionEvent.Error($"Read error: {ex.Message}"));
        }
    }

    private void Raise(SessionEvent evt)
    {
        try { EventRaised?.Invoke(evt); }
        catch { }
    }
}
