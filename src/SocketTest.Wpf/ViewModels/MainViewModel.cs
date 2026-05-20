using SocketTest.Wpf.Mvvm;

namespace SocketTest.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    public TcpClientTabViewModel TcpClient { get; } = new();
    public TcpServerTabViewModel TcpServer { get; } = new();
    public UdpTabViewModel Udp { get; } = new();

    public string Title { get; } = "SocketTest.NET — TCP/UDP tester";

    public async Task ShutdownAsync()
    {
        await TcpClient.DisposeAsync();
        await TcpServer.DisposeAsync();
        await Udp.DisposeAsync();
    }
}
