using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace WarehouseManagementApp.Converters;

public class LowStockToBrushConverter : IMultiValueConverter
{
    private static readonly SolidColorBrush LowStockBrush;

    static LowStockToBrushConverter()
    {
        LowStockBrush = new SolidColorBrush(Color.FromArgb(40, 244, 67, 54));
        LowStockBrush.Freeze();
    }

    public object Convert(object[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values != null && values.Length >= 2)
        {
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
            {
                return Brushes.Transparent;
            }

            decimal currentQty = values[0] is decimal c ? c : (values[0] != null && decimal.TryParse(values[0].ToString(), out var parsedC) ? parsedC : 0m);
            decimal minQty = values[1] is decimal m ? m : (values[1] != null && decimal.TryParse(values[1].ToString(), out var parsedM) ? parsedM : 0m);

            if (currentQty <= minQty)
            {
                return LowStockBrush;
            }
        }

        return Brushes.Transparent;
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        return targetTypes.Select(_ => Binding.DoNothing).ToArray();
    }
}