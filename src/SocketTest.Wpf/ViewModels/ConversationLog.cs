using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using SocketTest.Core;
using SocketTest.Wpf.Mvvm;

namespace SocketTest.Wpf.ViewModels;

public sealed class ConversationLog : ViewModelBase
{
    private const int MaxEntries = 5000;

    private readonly Queue<SessionEvent> _pending = new();
    private readonly object _gate = new();
    private bool _flushScheduled;

    public ObservableCollection<SessionEvent> Entries { get; } = new();

    public void Add(SessionEvent evt)
    {
        if (evt.Direction is SessionDirection.Received or SessionDirection.Sent)
        {
            var formatted = JsonFormatter.TryPrettyPrint(evt.Text);
            if (!ReferenceEquals(formatted, evt.Text))
                evt = evt with { Text = formatted };
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            AddSafe(evt);
            return;
        }

        bool schedule;
        lock (_gate)
        {
            _pending.Enqueue(evt);
            schedule = !_flushScheduled;
            if (schedule) _flushScheduled = true;
        }

        if (schedule)
            dispatcher.BeginInvoke(new Action(FlushNow), DispatcherPriority.Background);
    }

    private void FlushNow()
    {
        List<SessionEvent> batch;
        lock (_gate)
        {
            batch = new List<SessionEvent>(_pending);
            _pending.Clear();
            _flushScheduled = false;
        }

        if (batch.Count == 0) return;

        try
        {
            foreach (var e in batch)
                Entries.Add(e);
            while (Entries.Count > MaxEntries)
                Entries.RemoveAt(0);
        }
        catch
        {
        }
    }

    private void AddSafe(SessionEvent evt)
    {
        try
        {
            Entries.Add(evt);
            while (Entries.Count > MaxEntries)
                Entries.RemoveAt(0);
        }
        catch
        {
        }
    }

    public void Clear()
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.InvokeAsync(Entries.Clear);
        else
            Entries.Clear();
    }

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        foreach (var entry in Entries)
        {
            sb.Append('[').Append(entry.Timestamp.ToString("HH:mm:ss.fff")).Append("] ");
            sb.Append(entry.Direction switch
            {
                SessionDirection.Info => "INFO  ",
                SessionDirection.Sent => "SENT  ",
                SessionDirection.Received => "RECV  ",
                SessionDirection.Error => "ERROR ",
                _ => "      "
            });
            sb.AppendLine(entry.Text);
        }
        return sb.ToString();
    }
}
