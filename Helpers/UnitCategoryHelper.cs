using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Helpers;

public enum UnitMeasurementCategory
{
    Count, 
    Length,
    Weight, 
    Volume,
    Area, 
    Other 
}

public static class UnitCategoryHelper
{
    private readonly record struct UnitDescriptor(UnitMeasurementCategory Category, decimal Factor);

    private static readonly Dictionary<string, UnitDescriptor> KnownUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["км"] = new(UnitMeasurementCategory.Length, 1000m),
        ["кілометр"] = new(UnitMeasurementCategory.Length, 1000m),

        ["м"] = new(UnitMeasurementCategory.Length, 1m),
        ["метр"] = new(UnitMeasurementCategory.Length, 1m),

        ["дм"] = new(UnitMeasurementCategory.Length, 0.1m),
        ["дециметр"] = new(UnitMeasurementCategory.Length, 0.1m),

        ["см"] = new(UnitMeasurementCategory.Length, 0.01m),
        ["сантиметр"] = new(UnitMeasurementCategory.Length, 0.01m),

        ["мм"] = new(UnitMeasurementCategory.Length, 0.001m),
        ["міліметр"] = new(UnitMeasurementCategory.Length, 0.001m),

        ["м"] = new(UnitMeasurementCategory.Weight, 1000m),

        ["тонна"] = new(UnitMeasurementCategory.Weight, 1000m),

        ["ц"] = new(UnitMeasurementCategory.Weight, 100m),
        ["центнер"] = new(UnitMeasurementCategory.Weight, 100m),

        ["кг"] = new(UnitMeasurementCategory.Weight, 1m),
        ["kg"] = new(UnitMeasurementCategory.Weight, 1m),
        ["кілограм"] = new(UnitMeasurementCategory.Weight, 1m),
        ["kilogram"] = new(UnitMeasurementCategory.Weight, 1m),

        ["г"] = new(UnitMeasurementCategory.Weight, 0.001m),
        ["g"] = new(UnitMeasurementCategory.Weight, 0.001m),
        ["грам"] = new(UnitMeasurementCategory.Weight, 0.001m),
        ["gram"] = new(UnitMeasurementCategory.Weight, 0.001m),

        ["мг"] = new(UnitMeasurementCategory.Weight, 0.000001m),
        ["mg"] = new(UnitMeasurementCategory.Weight, 0.000001m),
        ["міліграм"] = new(UnitMeasurementCategory.Weight, 0.000001m),
        ["milligram"] = new(UnitMeasurementCategory.Weight, 0.000001m),
        
        ["м3"] = new(UnitMeasurementCategory.Volume, 1000m),
        ["m3"] = new(UnitMeasurementCategory.Volume, 1000m),
        ["куб.м"] = new(UnitMeasurementCategory.Volume, 1000m),
        ["кубічний метр"] = new(UnitMeasurementCategory.Volume, 1000m),

        ["л"] = new(UnitMeasurementCategory.Volume, 1m),
        ["l"] = new(UnitMeasurementCategory.Volume, 1m),
        ["дм3"] = new(UnitMeasurementCategory.Volume, 1m),
        ["літр"] = new(UnitMeasurementCategory.Volume, 1m),
        ["liter"] = new(UnitMeasurementCategory.Volume, 1m),
        ["litre"] = new(UnitMeasurementCategory.Volume, 1m),

        ["дл"] = new(UnitMeasurementCategory.Volume, 0.1m),
        ["dl"] = new(UnitMeasurementCategory.Volume, 0.1m),

        ["мл"] = new(UnitMeasurementCategory.Volume, 0.001m),
        ["ml"] = new(UnitMeasurementCategory.Volume, 0.001m),
        ["см3"] = new(UnitMeasurementCategory.Volume, 0.001m),
        ["куб.см"] = new(UnitMeasurementCategory.Volume, 0.001m),
        ["мілілітр"] = new(UnitMeasurementCategory.Volume, 0.001m),
        ["milliliter"] = new(UnitMeasurementCategory.Volume, 0.001m),

        ["га"] = new(UnitMeasurementCategory.Area, 10000m),
        ["ha"] = new(UnitMeasurementCategory.Area, 10000m),
        ["гектар"] = new(UnitMeasurementCategory.Area, 10000m),

        ["ар"] = new(UnitMeasurementCategory.Area, 100m),
        ["ar"] = new(UnitMeasurementCategory.Area, 100m),

        ["м2"] = new(UnitMeasurementCategory.Area, 1m),
        ["m2"] = new(UnitMeasurementCategory.Area, 1m),
        ["кв.м"] = new(UnitMeasurementCategory.Area, 1m),
        ["квадратний метр"] = new(UnitMeasurementCategory.Area, 1m),

        ["см2"] = new(UnitMeasurementCategory.Area, 0.0001m),
        ["cm2"] = new(UnitMeasurementCategory.Area, 0.0001m),
        ["кв.см"] = new(UnitMeasurementCategory.Area, 0.0001m),

        ["мм2"] = new(UnitMeasurementCategory.Area, 0.000001m),
        ["mm2"] = new(UnitMeasurementCategory.Area, 0.000001m),
        ["кв.мм"] = new(UnitMeasurementCategory.Area, 0.000001m),

        ["шт"] = new(UnitMeasurementCategory.Count, 1m),
        ["pcs"] = new(UnitMeasurementCategory.Count, 1m),
        ["штука"] = new(UnitMeasurementCategory.Count, 1m),
        ["piece"] = new(UnitMeasurementCategory.Count, 1m),

        ["уп"] = new(UnitMeasurementCategory.Count, 1m),
        ["pkg"] = new(UnitMeasurementCategory.Count, 1m),
        ["упаковка"] = new(UnitMeasurementCategory.Count, 1m),

        ["пач"] = new(UnitMeasurementCategory.Count, 1m),
        ["пачка"] = new(UnitMeasurementCategory.Count, 1m),

        ["ящ"] = new(UnitMeasurementCategory.Count, 1m),
        ["box"] = new(UnitMeasurementCategory.Count, 1m),
        ["ящик"] = new(UnitMeasurementCategory.Count, 1m),

        ["наб"] = new(UnitMeasurementCategory.Count, 1m),
        ["set"] = new(UnitMeasurementCategory.Count, 1m),
        ["набір"] = new(UnitMeasurementCategory.Count, 1m),

        ["компл"] = new(UnitMeasurementCategory.Count, 1m),
        ["комплект"] = new(UnitMeasurementCategory.Count, 1m),

        ["рул"] = new(UnitMeasurementCategory.Count, 1m),
        ["roll"] = new(UnitMeasurementCategory.Count, 1m),
        ["рулон"] = new(UnitMeasurementCategory.Count, 1m),

        ["бліст"] = new(UnitMeasurementCategory.Count, 1m),
        ["блістер"] = new(UnitMeasurementCategory.Count, 1m),

        ["бут"] = new(UnitMeasurementCategory.Count, 1m),
        ["пляшка"] = new(UnitMeasurementCategory.Count, 1m),

        ["пал"] = new(UnitMeasurementCategory.Count, 1m),
        ["палета"] = new(UnitMeasurementCategory.Count, 1m),

        ["міш"] = new(UnitMeasurementCategory.Count, 1m),
        ["мішок"] = new(UnitMeasurementCategory.Count, 1m),

        ["боч"] = new(UnitMeasurementCategory.Count, 1m),
        ["бочка"] = new(UnitMeasurementCategory.Count, 1m)
    };

    private static bool TryResolveDescriptor(Unit? unit, out UnitDescriptor descriptor)
    {
        if (unit != null)
        {
            if (!string.IsNullOrWhiteSpace(unit.Code) &&
                KnownUnits.TryGetValue(unit.Code.Trim(), out descriptor))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(unit.Name) &&
                KnownUnits.TryGetValue(unit.Name.Trim(), out descriptor))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(unit.Name))
            {
                string lowerName = unit.Name.Trim();
                foreach (var (key, value) in KnownUnits)
                {
                    if (key.Length > 2 && lowerName.Contains(key, StringComparison.OrdinalIgnoreCase))
                    {
                        descriptor = value;
                        return true;
                    }
                }
            }
        }

        descriptor = default;
        return false;
    }

    public static UnitMeasurementCategory GetCategory(Unit? unit)
    {
        if (unit == null) return UnitMeasurementCategory.Other;
        if (TryResolveDescriptor(unit, out var descriptor)) return descriptor.Category;
        return unit.IsFractional ? UnitMeasurementCategory.Other : UnitMeasurementCategory.Count;
    }

    public static decimal GetBaseFactor(Unit? unit)
    {
        return TryResolveDescriptor(unit, out var descriptor) ? descriptor.Factor : 1m;
    }

    public static string GetCategoryDisplayName(UnitMeasurementCategory category) => category switch
    {
        UnitMeasurementCategory.Count => "Штучні товари (кількість)",
        UnitMeasurementCategory.Length => "Довжина",
        UnitMeasurementCategory.Weight => "Маса / Вага",
        UnitMeasurementCategory.Volume => "Об'єм",
        UnitMeasurementCategory.Area => "Площа",
        _ => "Інша категорія"
    };

    public static bool AreCompatible(Unit? a, Unit? b)
    {
        if (a == null || b == null) return false;
        if (a.Id != 0 && b.Id != 0 && a.Id == b.Id) return true;
        return GetCategory(a) == GetCategory(b);
    }

    public static decimal ConvertQuantity(decimal quantity, Unit? fromUnit, Unit? toUnit)
    {
        if (fromUnit == null || toUnit == null || fromUnit.Id == toUnit.Id)
            return quantity;

        if (!AreCompatible(fromUnit, toUnit))
            throw new InvalidOperationException("Неможливо конвертувати кількість між несумісними категоріями одиниць виміру.");

        decimal fromFactor = GetBaseFactor(fromUnit);
        decimal toFactor = GetBaseFactor(toUnit);

        if (toFactor <= 0m) return quantity;

        decimal converted = quantity * (fromFactor / toFactor);
        return toUnit.IsFractional ? Math.Round(converted, 3) : Math.Round(converted, 0);
    }

    public static decimal ConvertUnitPrice(decimal pricePerFromUnit, Unit? fromUnit, Unit? toUnit)
    {
        if (fromUnit == null || toUnit == null || fromUnit.Id == toUnit.Id)
            return pricePerFromUnit;

        if (!AreCompatible(fromUnit, toUnit))
            return pricePerFromUnit;

        decimal fromFactor = GetBaseFactor(fromUnit);
        decimal toFactor = GetBaseFactor(toUnit);

        if (fromFactor <= 0m) return pricePerFromUnit;

        decimal converted = pricePerFromUnit * (toFactor / fromFactor);
        return Math.Round(converted, 2);
    }
}
