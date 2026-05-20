using System.Windows;
using System.Windows.Controls;
using SocketTest.Wpf.ViewModels;

namespace SocketTest.Wpf.Views;

public partial class TcpClientView : UserControl
{
    public TcpClientView()
    {
        InitializeComponent();
    }

    private void OnEntrySampleClick(object sender, RoutedEventArgs e)
    {
        var dlg = new EntrySampleWindow
        {
            Owner = Window.GetWindow(this)
        };

        if (dlg.ShowDialog() == true
            && !string.IsNullOrWhiteSpace(dlg.SelectedJson)
            && DataContext is TcpClientTabViewModel vm)
        {
            vm.SendText = dlg.SelectedJson;
        }
    }
}
