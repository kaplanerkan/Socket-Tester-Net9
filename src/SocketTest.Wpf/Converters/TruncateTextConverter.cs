using System.Globalization;
using System.Windows.Data;

namespace SocketTest.Wpf.Converters;

/// <summary>
/// Caps how much of a log entry is rendered in the conversation list so a single
/// huge payload cannot flood the UI. The full text stays in the model — copy
/// commands and double-click always use the untruncated <c>SessionEvent.Text</c>.
/// </summary>
public sealed class TruncateTextConverter : IValueConverter
{
    public int MaxChars { get; set; } = 1500;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text) return string.Empty;
        if (text.Length <= MaxChars) return text;
        return string.Concat(
            text.AsSpan(0, MaxChars),
            $" … [+{text.Length - MaxChars:N0} chars — double-click to copy full text]");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
