using System.Windows;
using System.Windows.Input;

namespace SocketTest.Wpf.Views;

public partial class EntrySampleWindow : Window
{
    private static readonly string[] _samples =
    {
        "[syncUnites] Istek: [{\"command\":33,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncTaxes] Istek: [{\"command\":34,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncDeposites] Istek: [{\"command\":35,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncProductGroups] Istek: [{\"command\":30,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncProductPricesWithProgress] Istek: [{\"command\":32,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{\"groupid\":0,\"limit\":5,\"offset\":0}]",
        "[syncProductsWithProgress] Istek: [{\"command\":31,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{\"groupid\":0,\"limit\":5,\"offset\":0}]",
        "[syncSuppliers] Istek: [{\"command\":36,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncCustomers] Istek: [{\"command\":37,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncSettings] Istek: [{\"command\":14,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncWarehouses] Istek: [{\"command\":170,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]",
        "[syncTime] Istek: [{\"command\":190,\"device\":\"6b984d4888f09aadunknown\",\"userid\":-1},{}]"
    };

    public string? SelectedJson { get; private set; }

    public EntrySampleWindow()
    {
        InitializeComponent();
        SamplesBox.Text = string.Join(Environment.NewLine, _samples);
    }

    private void OnSamplesDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var lineIndex = SamplesBox.GetLineIndexFromCharacterIndex(SamplesBox.CaretIndex);
        if (lineIndex < 0) return;
        var line = SamplesBox.GetLineText(lineIndex);
        if (string.IsNullOrWhiteSpace(line)) return;

        var bracketStart = line.IndexOf('[');
        // first [ may be the label like [syncUnites], find the payload's leading [
        var payloadStart = line.IndexOf(": ", StringComparison.Ordinal);
        if (payloadStart >= 0)
            bracketStart = line.IndexOf('[', payloadStart);

        if (bracketStart < 0) return;
        SelectedJson = line[bracketStart..].TrimEnd('\r', '\n');
        DialogResult = true;
        Close();
    }

    private void OnCopySelectionClick(object sender, RoutedEventArgs e)
    {
        var text = string.IsNullOrEmpty(SamplesBox.SelectedText)
            ? SamplesBox.Text
            : SamplesBox.SelectedText;
        try { Clipboard.SetText(text); }
        catch { }
    }

    private void OnCopyAllClick(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(SamplesBox.Text); }
        catch { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
