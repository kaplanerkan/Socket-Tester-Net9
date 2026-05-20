using System.IO;
using Microsoft.Win32;
using SocketTest.Core;
using SocketTest.Wpf.Mvvm;

namespace SocketTest.Wpf.ViewModels;

public sealed class TcpServerTabViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly TcpServerSession _session = new();

    private string _host = "0.0.0.0";
    private string _port = "1454";
    private string _sendText = string.Empty;
    private bool _isListening;
    private PayloadEncoding _encoding = PayloadEncoding.Utf8;
    private LineEnding _lineEnding = LineEnding.CrLf;

    public TcpServerTabViewModel()
    {
        _session.EventRaised += evt => Log.Add(evt);

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsListening);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsListening);
        SendCommand = new AsyncRelayCommand(SendAsync, () => IsListening);
        ClearCommand = new RelayCommand(() => Log.Clear());
        SaveCommand = new RelayCommand(SaveConversation);
    }

    public ConversationLog Log { get; } = new();

    public string Host { get => _host; set => SetField(ref _host, value); }
    public string Port { get => _port; set => SetField(ref _port, value); }
    public string SendText { get => _sendText; set => SetField(ref _sendText, value); }

    public bool IsListening
    {
        get => _isListening;
        private set
        {
            if (SetField(ref _isListening, value))
            {
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                SendCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public PayloadEncoding Encoding
    {
        get => _encoding;
        set { if (SetField(ref _encoding, value)) _session.Encoding = value; }
    }

    public LineEnding LineEnding
    {
        get => _lineEnding;
        set { if (SetField(ref _lineEnding, value)) _session.LineEnding = value; }
    }

    public IReadOnlyList<PayloadEncoding> EncodingOptions { get; } = Enum.GetValues<PayloadEncoding>();
    public IReadOnlyList<LineEnding> LineEndingOptions { get; } = Enum.GetValues<LineEnding>();

    public AsyncRelayCommand StartCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand SendCommand { get; }
    public RelayCommand ClearCommand { get; }
    public RelayCommand SaveCommand { get; }

    private async Task StartAsync()
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
            await _session.StartAsync(Host, port);
            IsListening = true;
        }
        catch (Exception ex)
        {
            Log.Add(SessionEvent.Error($"Start failed: {ex.Message}"));
        }
    }

    private async Task StopAsync()
    {
        await _session.StopAsync();
        IsListening = false;
    }

    private async Task SendAsync()
    {
        try { await _session.SendAsync(SendText); }
        catch (Exception ex) { Log.Add(SessionEvent.Error($"Send failed: {ex.Message}")); }
    }

    private void SaveConversation()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"server_conversation_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, Log.ToPlainText());
    }

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();
}
