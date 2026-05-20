using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using SocketTest.Core;

namespace SocketTest.Wpf.Converters;

public sealed class DirectionToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        SessionDirection.Info => Brushes.Gray,
        SessionDirection.Sent => Brushes.SteelBlue,
        SessionDirection.Received => Brushes.DarkGreen,
        SessionDirection.Error => Brushes.Firebrick,
        _ => Brushes.Black
    };

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
