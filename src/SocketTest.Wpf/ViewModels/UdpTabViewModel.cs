using System.IO;
using Microsoft.Win32;
using SocketTest.Core;
using SocketTest.Wpf.Mvvm;

namespace SocketTest.Wpf.ViewModels;

public sealed class UdpTabViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly UdpSession _session = new();

    private string _listenPort = "1454";
    private string _targetHost = "127.0.0.1";
    private string _targetPort = "1454";
    private string _sendText = string.Empty;
    private bool _isListening;
    private PayloadEncoding _encoding = PayloadEncoding.Utf8;
    private LineEnding _lineEnding = LineEnding.CrLf;

    public UdpTabViewModel()
    {
        _session.EventRaised += evt => Log.Add(evt);

        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsListening);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsListening);
        SendCommand = new AsyncRelayCommand(SendAsync);
        ClearCommand = new RelayCommand(() => Log.Clear());
        SaveCommand = new RelayCommand(SaveConversation);
    }

    public ConversationLog Log { get; } = new();

    public string ListenPort { get => _listenPort; set => SetField(ref _listenPort, value); }
    public string TargetHost { get => _targetHost; set => SetField(ref _targetHost, value); }
    public string TargetPort { get => _targetPort; set => SetField(ref _targetPort, value); }
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
        if (!int.TryParse(ListenPort, out var port) || port <= 0 || port > 65535)
        {
            Log.Add(SessionEvent.Error($"Invalid listen port: {ListenPort}"));
            return;
        }
        try
        {
            _session.Encoding = Encoding;
            _session.LineEnding = LineEnding;
            await _session.StartListenerAsync(port);
            IsListening = true;
        }
        catch (Exception ex)
        {
            Log.Add(SessionEvent.Error($"UDP start failed: {ex.Message}"));
        }
    }

    private async Task StopAsync()
    {
        await _session.StopAsync();
        IsListening = false;
    }

    private async Task SendAsync()
    {
        if (!int.TryParse(TargetPort, out var port) || port <= 0 || port > 65535)
        {
            Log.Add(SessionEvent.Error($"Invalid target port: {TargetPort}"));
            return;
        }
        try { await _session.SendAsync(TargetHost, port, SendText); }
        catch (Exception ex) { Log.Add(SessionEvent.Error($"Send failed: {ex.Message}")); }
    }

    private void SaveConversation()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"udp_conversation_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (dlg.ShowDialog() != true) return;
        File.WriteAllText(dlg.FileName, Log.ToPlainText());
    }

    public async ValueTask DisposeAsync() => await _session.DisposeAsync();
}
