using System.Windows;
using SocketTest.Wpf.ViewModels;

namespace SocketTest.Wpf;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;
        Closed += OnClosed;
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        await _vm.ShutdownAsync();
    }
}
