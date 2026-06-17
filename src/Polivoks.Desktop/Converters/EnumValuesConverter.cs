using System.Globalization;
using System.Windows.Data;

namespace Polivoks.Desktop.Converters;

public sealed class EnumValuesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public static Array GetValues(Type enumType) => Enum.GetValues(enumType);
}
