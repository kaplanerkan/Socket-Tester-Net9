using System.Net;
using System.Net.Sockets;

namespace SocketTest.Core;

public sealed class UdpSession : IAsyncDisposable
{
    public event Action<SessionEvent>? EventRaised;

    private UdpClient? _listener;
    private CancellationTokenSource? _cts;
    private Task? _readLoop;

    public bool IsListening => _listener is not null;

    public PayloadEncoding Encoding { get; set; } = PayloadEncoding.Utf8;
    public LineEnding LineEnding { get; set; } = LineEnding.CrLf;

    public Task StartListenerAsync(int port)
    {
        if (IsListening)
            throw new InvalidOperationException("Already listening.");

        _listener = new UdpClient(port);
        _cts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));
        Raise(SessionEvent.Info($"UDP listening on port {port}..."));
        return Task.CompletedTask;
    }

    public async Task SendAsync(string host, int port, string text, CancellationToken ct = default)
    {
        var withEnding = LineEnding.AppendLineEnding(text);
        var bytes = Encoding.Encode(withEnding);
        using var tx = new UdpClient();
        await tx.SendAsync(bytes, bytes.Length, host, port).WaitAsync(ct).ConfigureAwait(false);
        Raise(SessionEvent.Sent($"-> {host}:{port}  {Encoding.Decode(bytes)}"));
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
            await _cts.CancelAsync().ConfigureAwait(false);

        try { _listener?.Close(); } catch { }
        try { if (_readLoop is not null) await _readLoop.ConfigureAwait(false); } catch { }

        _listener?.Dispose();
        _listener = null;
        _cts?.Dispose();
        _cts = null;
        _readLoop = null;
        Raise(SessionEvent.Info("UDP listener stopped."));
    }

    public async ValueTask DisposeAsync() => await StopAsync().ConfigureAwait(false);

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        if (_listener is null) return;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _listener.ReceiveAsync(ct).ConfigureAwait(false);
                var text = Encoding.Decode(result.Buffer);
                Raise(SessionEvent.Received($"<- {result.RemoteEndPoint}  {text}"));
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
            Raise(SessionEvent.Error($"UDP read error: {ex.Message}"));
        }
    }

    private void Raise(SessionEvent evt)
    {
        try { EventRaised?.Invoke(evt); }
        catch { }
    }
}
