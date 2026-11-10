using System.Globalization;

namespace WarehouseManagementApp.Helpers;

public static class NumericParserHelper
{
    public static bool TryParseNonNegativeDecimal(string? input, out decimal result)
    {
        result = 0m;
        if (string.IsNullOrWhiteSpace(input)) return false;

        string normalized = input.Trim().Replace(',', '.');
        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) && parsed >= 0m)
        {
            result = parsed;
            return true;
        }
        return false;
    }

    public static decimal ParseNonNegativeOrDefault(string? input, decimal defaultValue = 0m)
    {
        return TryParseNonNegativeDecimal(input, out decimal val) ? val : defaultValue;
    }

    public static string FormatPrice(decimal price) =>
        price.ToString("0.00", CultureInfo.InvariantCulture);

    public static string FormatQuantity(decimal quantity) =>
        quantity.ToString("0.###", CultureInfo.InvariantCulture);
}

