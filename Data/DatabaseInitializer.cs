using Microsoft.EntityFrameworkCore;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Data;

public static class DatabaseInitializer
{
    private static readonly Unit[] DefaultUnits =
    {
        new() { Code = "шт", Name = "Штука", IsFractional = false },
        new() { Code = "уп", Name = "Упаковка", IsFractional = false },
        new() { Code = "кг", Name = "Кілограм", IsFractional = true },
        new() { Code = "г", Name = "Грам", IsFractional = true },
        new() { Code = "л", Name = "Літр", IsFractional = true },
        new() { Code = "мл", Name = "Мілілітр", IsFractional = true },
        new() { Code = "м", Name = "Метр", IsFractional = true },
        new() { Code = "см", Name = "Сантиметр", IsFractional = true },
        new() { Code = "мм", Name = "Міліметр", IsFractional = true }
    };

    public static void Initialize(IDbContextFactory<WarehouseDbContext> contextFactory)
    {
        using var db = contextFactory.CreateDbContext();
        db.Database.EnsureCreated();

        try
        {
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
            db.Database.ExecuteSqlRaw("PRAGMA journal_mode = WAL;");
            db.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");
        }
        catch
        {
        }

        SeedDefaultUnits(db);
    }

    private static void SeedDefaultUnits(WarehouseDbContext db)
    {
        var existingCodes = db.Units
            .AsNoTracking()
            .Select(u => u.Code)
            .ToList()
            .Select(c => c.Trim().ToLowerInvariant())
            .ToHashSet();

        var missingUnits = DefaultUnits
            .Where(u => !existingCodes.Contains(u.Code.ToLowerInvariant()))
            .Select(u => new Unit
            {
                Code = u.Code,
                Name = u.Name,
                IsFractional = u.IsFractional
            })
            .ToList();

        if (missingUnits.Count > 0)
        {
            db.Units.AddRange(missingUnits);
            db.SaveChanges();
        }
    }
}

