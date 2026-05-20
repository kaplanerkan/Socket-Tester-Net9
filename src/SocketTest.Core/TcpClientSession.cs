using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SocketTest.Core;

public sealed class TcpClientSession : IAsyncDisposable
{
    public event Action<SessionEvent>? EventRaised;

    private TcpClient? _client;
    private Stream? _stream;
    private CancellationTokenSource? _cts;
    private Task? _readLoop;

    public bool IsConnected => _client?.Connected == true;

    public PayloadEncoding Encoding { get; set; } = PayloadEncoding.Utf8;
    public LineEnding LineEnding { get; set; } = LineEnding.CrLf;

    public Func<X509Certificate?, bool>? CertificateTrustPrompt { get; set; }

    public async Task ConnectAsync(string host, int port, bool useSsl, CancellationToken ct = default)
    {
        if (IsConnected)
            throw new InvalidOperationException("Already connected.");

        Raise(SessionEvent.Info($"Connecting to {host}:{port}{(useSsl ? " (SSL)" : string.Empty)}..."));

        _client = new TcpClient();
        await _client.ConnectAsync(host, port, ct).ConfigureAwait(false);

        var network = _client.GetStream();
        if (useSsl)
        {
            var ssl = new SslStream(network, leaveInnerStreamOpen: false, ValidateCertificate);
            await ssl.AuthenticateAsClientAsync(host).ConfigureAwait(false);
            _stream = ssl;
        }
        else
        {
            _stream = network;
        }

        _cts = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(_cts.Token));

        Raise(SessionEvent.Info($"Connected to {host}:{port}."));
    }

    public async Task SendAsync(string text, CancellationToken ct = default)
    {
        if (_stream is null || !IsConnected)
            throw new InvalidOperationException("Not connected.");

        var withEnding = LineEnding.AppendLineEnding(text);
        var bytes = Encoding.Encode(withEnding);
        await _stream.WriteAsync(bytes, ct).ConfigureAwait(false);
        await _stream.FlushAsync(ct).ConfigureAwait(false);
        Raise(SessionEvent.Sent(Encoding == PayloadEncoding.Hex
            ? Encoding.Decode(bytes)
            : withEnding));
    }

    public async Task DisconnectAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync().ConfigureAwait(false);
        }

        try
        {
            if (_readLoop is not null)
                await _readLoop.ConfigureAwait(false);
        }
        catch
        {
        }

        _stream?.Dispose();
        _client?.Dispose();
        _stream = null;
        _client = null;
        _cts?.Dispose();
        _cts = null;
        _readLoop = null;

        Raise(SessionEvent.Info("Disconnected."));
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);

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
                    Raise(SessionEvent.Info("Remote endpoint closed the connection."));
                    return;
                }
                var text = Encoding.Decode(buffer.AsSpan(0, read));
                Raise(SessionEvent.Received(text));
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

    private bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None) return true;
        var prompt = CertificateTrustPrompt;
        if (prompt is null) return false;
        var trust = prompt(certificate);
        if (trust)
            Raise(SessionEvent.Info($"Certificate accepted by user despite: {errors}"));
        return trust;
    }

    private void Raise(SessionEvent evt)
    {
        try { EventRaised?.Invoke(evt); }
        catch { }
    }
}
