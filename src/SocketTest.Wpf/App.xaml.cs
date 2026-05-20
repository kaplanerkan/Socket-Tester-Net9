using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SocketTest.Wpf;

public partial class App : Application
{
    private static readonly HashSet<string> _reportedSignatures = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Report("UI dispatcher", e.Exception);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Report("AppDomain", ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogOnly("Unobserved task", e.Exception);
        e.SetObserved();
    }

    private static void LogOnly(string source, Exception ex)
    {
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(logPath, $"{DateTime.Now:O} [{source}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch
        {
        }
    }

    private static void Report(string source, Exception ex)
    {
        var text = $"[{source}]\n{ex.GetType().FullName}: {ex.Message}\n\n{ex.StackTrace}";
        try
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(logPath, $"{DateTime.Now:O}\n{text}\n\n");
        }
        catch
        {
        }

        var signature = $"{source}|{ex.GetType().FullName}|{ex.Message}";
        lock (_reportedSignatures)
        {
            if (!_reportedSignatures.Add(signature)) return;
        }
        MessageBox.Show(text, "SocketTest.NET — Unhandled exception", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
