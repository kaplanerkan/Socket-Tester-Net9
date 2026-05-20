using System.IO;
using System.Windows;
using Microsoft.Win32;
using SocketTest.Core;
using SocketTest.Wpf.Mvvm;

namespace SocketTest.Wpf.ViewModels;

public sealed class TcpClientTabViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly TcpClientSession _session = new();

    private string _host = "192.168.1.";
    private string _port = "1454";
    private string _sendText = string.Empty;
    private bool _useSsl;
    private bool _isConnected;
    private PayloadEncoding _encoding = PayloadEncoding.Utf8;
    private LineEnding _lineEnding = LineEnding.CrLf;

    public TcpClientTabViewModel()
    {
        _session.EventRaised += evt => Log.Add(evt);
        _session.CertificateTrustPrompt = cert =>
        {
            var msg = cert is null
                ? "The remote certificate is not trusted. Proceed anyway?"
                : $"Certificate subject: {cert.Subject}\nIssuer: {cert.Issuer}\n\nTrust and continue?";
            return MessageBox.Show(msg, "Untrusted certificate", MessageBoxButton.YesNo, MessageBoxImage.Warning)
                   == MessageBoxResult.Yes;
        };

        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !IsConnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsConnected);
        SendCommand = new AsyncRelayCommand(SendAsync, () => IsConnected);
        ClearCommand = new RelayCommand(() => Log.Clear());
        SaveCommand = new RelayCommand(SaveConversation);
    }

    public ConversationLog Log { get; } = new();

    public string Host { get => _host; set => SetField(ref _host, value); }
    public string Port { get => _port; set => SetField(ref _port, value); }
    public string SendText { get => _sendText; set => SetField(ref _sendText, value); }
    public bool UseSsl { get => _useSsl; set => SetField(ref _useSsl, value); }

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetField(ref _isConnected, value))
            {
                ConnectCommand.RaiseCanExecuteChanged();
                DisconnectCommand.RaiseCanExecuteChanged();
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public PayloadEncoding Encoding
    {
        get => _encoding;
        set
        {
            if (SetField(ref _encoding, value))
                _session.Encoding = value;
        }
    }

    public LineEnding LineEnding
    {
        get => _lineEnding;
        set
        {
            if (SetField(ref _lineEnding, value))
                _session.LineEnding = value;
        }
    }

    public IReadOnlyList<PayloadEncoding> EncodingOptions { get; } = Enum.GetValues<PayloadEncoding>();
    public IReadOnlyList<LineEnding> LineEndingOptions { get; } = Enum.GetValues<LineEnding>();

    public AsyncRelayCommand ConnectCommand { get; }
    public AsyncRelayCommand DisconnectCommand { get; }
    public AsyncRelayCommand SendCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveCommand { get; }

    private async Task ConnectAsync()
    {
        if (!int.TryParse(Port, out var port) || port <= 0 || port > 65535)
        {
            Log.Add(SessionEvent.Error($"Invalid port: {Port}"));
            return;
        }
        try
        {
            _session.Encoding = Encoding;
            _session.LineEnding = LineEnding;
            await _session.ConnectAsync(Host, port, UseSsl);
            IsConnected = true;
        }
        catch (Exception ex)
        {
            Log.Add(SessionEvent.Error($"Connect failed: {ex.Message}"));
        }
    }

    private async Task DisconnectAsync()
    {
        await _session.DisconnectAsync();
        IsConnected = false;
    }

    private async Task SendAsync()
    {
        try
        {
            await _session.SendAsync(SendText);
        }
        catch (Exception ex)
        {
            Log.Add(SessionEvent.Error($"Send failed: {ex.Message}"));
            IsConnected = _session.IsConnected;
        }
    }

    private void SaveConversation()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"conversation_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, Log.ToPlainText());
    }

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();
}
