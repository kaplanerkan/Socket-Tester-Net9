using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using SocketTest.Core;
using SocketTest.Wpf.ViewModels;

namespace SocketTest.Wpf.Views;

public partial class ConversationView : UserControl
{
    private bool _scrollPending;

    public ConversationView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ConversationLog oldLog)
            oldLog.Entries.CollectionChanged -= OnEntriesChanged;
        if (e.NewValue is ConversationLog newLog)
            newLog.Entries.CollectionChanged += OnEntriesChanged;
    }

    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add) return;
        if (_scrollPending) return;
        _scrollPending = true;
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _scrollPending = false;
            try
            {
                if (LogList.Items.Count == 0) return;
                var last = LogList.Items[LogList.Items.Count - 1];
                LogList.ScrollIntoView(last);
            }
            catch
            {
            }
        }), DispatcherPriority.Background);
    }

    private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        CopySelectedText();
        e.Handled = true;
    }

    private void OnCopySelectedClick(object sender, RoutedEventArgs e) => CopySelectedText();

    private void OnCopySelectedFullClick(object sender, RoutedEventArgs e) => CopySelectedFull();

    private void OnCopyAllClick(object sender, RoutedEventArgs e) => CopyAll();

    private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LogList.SelectedItem is SessionEvent)
            CopySelectedText();
    }

    private void CopySelectedText()
    {
        var items = GetSelectedEvents();
        if (items.Count == 0) return;
        var text = items.Count == 1
            ? items[0].Text
            : string.Join(Environment.NewLine, items.ConvertAll(x => x.Text));
        SetClipboard(text);
    }

    private void CopySelectedFull()
    {
        var items = GetSelectedEvents();
        if (items.Count == 0) return;
        SetClipboard(FormatFull(items));
    }

    private void CopyAll()
    {
        var items = new List<SessionEvent>();
        foreach (var item in LogList.Items)
            if (item is SessionEvent evt) items.Add(evt);
        if (items.Count == 0) return;
        SetClipboard(FormatFull(items));
    }

    private List<SessionEvent> GetSelectedEvents()
    {
        var list = new List<SessionEvent>();
        foreach (var item in LogList.SelectedItems)
            if (item is SessionEvent evt) list.Add(evt);
        return list;
    }

    private static string FormatFull(IEnumerable<SessionEvent> events)
    {
        var sb = new StringBuilder();
        foreach (var evt in events)
        {
            sb.Append('[').Append(evt.Timestamp.ToString("HH:mm:ss.fff")).Append("] ");
            sb.Append(evt.Direction switch
            {
                SessionDirection.Info => "INFO  ",
                SessionDirection.Sent => "SENT  ",
                SessionDirection.Received => "RECV  ",
                SessionDirection.Error => "ERROR ",
                _ => "      "
            });
            sb.AppendLine(evt.Text);
        }
        return sb.ToString();
    }

    private static void SetClipboard(string text)
    {
        try { Clipboard.SetText(text); }
        catch { }
    }
}
