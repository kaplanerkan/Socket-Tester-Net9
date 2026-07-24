using System.Windows;
using System.Windows.Controls;

namespace SocketTest.Wpf.Views;

/// <summary>
/// Keeps a send TextBox single-line: pasted text has CR/LF collapsed before it
/// lands in the box. AcceptsReturn="False" only blocks the Enter key — without
/// this, a multi-line paste makes the auto-sized TextBox grow to hundreds of
/// lines and squeeze the rest of the layout out of the window.
/// </summary>
public static class SingleLineBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled", typeof(bool), typeof(SingleLineBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);

    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box) return;
        if ((bool)e.NewValue)
        {
            box.MaxLines = 1;
            DataObject.AddPastingHandler(box, OnPasting);
        }
        else
        {
            DataObject.RemovePastingHandler(box, OnPasting);
        }
    }

    private static void OnPasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText)) return;
        var text = e.DataObject.GetData(DataFormats.UnicodeText) as string ?? string.Empty;
        if (!text.Contains('\n') && !text.Contains('\r')) return;

        var flattened = text.Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
        e.CancelCommand();
        if (sender is TextBox box)
        {
            var start = box.SelectionStart;
            box.SelectedText = flattened;
            box.SelectionStart = start + flattened.Length;
            box.SelectionLength = 0;
        }
    }
}
