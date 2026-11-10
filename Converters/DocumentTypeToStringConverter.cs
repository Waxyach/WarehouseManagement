using System.Globalization;
using System.Windows.Data;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Converters;

[ValueConversion(typeof(DocumentType), typeof(string))]
public class DocumentTypeToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DocumentType docType)
        {
            return docType switch
            {
                DocumentType.Receipt => "Прибуткова накладна",
                DocumentType.Expense => "Видаткова накладна",
                DocumentType.WriteOff => "Акт списання",
                _ => value.ToString() ?? string.Empty
            };
        }
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}