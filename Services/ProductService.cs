using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementApp.Data;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<WarehouseDbContext> _contextFactory;

    public ProductService(IDbContextFactory<WarehouseDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Category> CreateCategoryAsync(Category category)
    {
        if (category == null) throw new ArgumentNullException(nameof(category));

        string trimmedName = category.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Назва категорії не може бути порожньою.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingNames = await context.Categories
            .AsNoTracking()
            .Select(c => c.Name)
            .ToListAsync();

        if (existingNames.Any(n => string.Equals(n.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Категорія «{trimmedName}» вже існує.");

        var entity = new Category
        {
            Name = trimmedName,
            ColorHex = string.IsNullOrWhiteSpace(category.ColorHex) ? "#3B82F6" : category.ColorHex.Trim()
        };

        context.Categories.Add(entity);
        await context.SaveChangesAsync();

        category.Id = entity.Id;
        category.Name = entity.Name;
        category.ColorHex = entity.ColorHex;
        return entity;
    }

    public async Task UpdateCategoryAsync(Category category)
    {
        if (category == null) throw new ArgumentNullException(nameof(category));

        string trimmedName = category.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Назва категорії не може бути порожньою.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Categories.FindAsync(category.Id);
        if (existing == null)
            throw new KeyNotFoundException($"Категорію з ID {category.Id} не знайдено.");

        var otherNames = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id != category.Id)
            .Select(c => c.Name)
            .ToListAsync();

        if (otherNames.Any(n => string.Equals(n.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Категорія «{trimmedName}» вже існує.");

        existing.Name = trimmedName;
        existing.ColorHex = string.IsNullOrWhiteSpace(category.ColorHex) ? "#3B82F6" : category.ColorHex.Trim();

        await context.SaveChangesAsync();
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var category = await context.Categories.FindAsync(id);
        if (category == null)
            return (false, "Категорію не знайдено.");

        bool hasProducts = await context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
            return (false, "Неможливо видалити категорію, до якої прив'язані товари.");

        context.Categories.Remove(category);
        await context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var list = await context.Categories
            .AsNoTracking()
            .ToListAsync();

        return list
            .OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        string normalizedSku = product.Sku?.Trim() ?? string.Empty;
        string trimmedName = product.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSku) || string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Артикул (SKU) та назва товару є обов'язковими.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingSkus = await context.Products
            .AsNoTracking()
            .Select(p => p.Sku)
            .ToListAsync();

        if (existingSkus.Any(s => string.Equals(s.Trim(), normalizedSku, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Товар з артикулом «{normalizedSku}» вже існує.");

        var entity = new Product
        {
            Sku = normalizedSku,
            Name = trimmedName,
            CategoryId = product.CategoryId,
            UnitId = product.UnitId,
            PurchasePrice = Math.Max(0m, product.PurchasePrice),
            RetailPrice = Math.Max(0m, product.RetailPrice),
            MinQuantity = Math.Max(0m, product.MinQuantity),
            StockBalance = new StockBalance
            {
                CurrentQuantity = product.StockBalance?.CurrentQuantity ?? 0m,
                LastUpdated = DateTime.UtcNow
            }
        };

        context.Products.Add(entity);
        await context.SaveChangesAsync();

        product.Id = entity.Id;
        return entity;
    }

    public async Task UpdateProductAsync(Product product)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        string normalizedSku = product.Sku?.Trim() ?? string.Empty;
        string trimmedName = product.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedSku) || string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Артикул (SKU) та назва товару не можуть бути порожніми.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var existing = await context.Products
            .Include(p => p.Unit)
            .Include(p => p.StockBalance)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (existing == null)
            throw new KeyNotFoundException($"Товар з ID {product.Id} не знайдено.");

        var otherSkus = await context.Products
            .AsNoTracking()
            .Where(p => p.Id != product.Id)
            .Select(p => p.Sku)
            .ToListAsync();

        if (otherSkus.Any(s => string.Equals(s.Trim(), normalizedSku, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Артикул «{normalizedSku}» вже використовується іншим товаром.");

        if (!string.Equals(existing.Name, trimmedName, StringComparison.OrdinalIgnoreCase))
        {
            bool isUsed = await context.StockDocumentItems.AnyAsync(i => i.ProductId == product.Id);
            if (isUsed)
            {
                throw new InvalidOperationException("Неможливо змінити назву товару, оскільки він уже використовується у накладних.");
            }
        }

        if (existing.UnitId != product.UnitId)
        {
            var oldUnit = existing.Unit ?? await context.Units.FindAsync(existing.UnitId);
            var newUnit = await context.Units.FindAsync(product.UnitId);

            if (oldUnit != null && newUnit != null)
            {
                if (!UnitCategoryHelper.AreCompatible(oldUnit, newUnit))
                {
                    var oldCat = UnitCategoryHelper.GetCategory(oldUnit);
                    var newCat = UnitCategoryHelper.GetCategory(newUnit);
                    throw new InvalidOperationException(
                        $"Неможливо перевести одиницю виміру «{oldUnit.Name}» ({UnitCategoryHelper.GetCategoryDisplayName(oldCat)}) " +
                        $"в «{newUnit.Name}» ({UnitCategoryHelper.GetCategoryDisplayName(newCat)}). " +
                        $"Одиницю виміру можна змінювати лише в межах однієї категорії (не можна перевести сантиметри в штуки).");
                }

                if (existing.StockBalance != null && existing.StockBalance.CurrentQuantity > 0)
                {
                    existing.StockBalance.CurrentQuantity = UnitCategoryHelper.ConvertQuantity(
                        existing.StockBalance.CurrentQuantity,
                        oldUnit,
                        newUnit);
                    existing.StockBalance.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        existing.Sku = normalizedSku;
        existing.Name = trimmedName;
        existing.CategoryId = product.CategoryId;
        existing.UnitId = product.UnitId;
        existing.PurchasePrice = Math.Max(0m, product.PurchasePrice);
        existing.RetailPrice = Math.Max(0m, product.RetailPrice);
        existing.MinQuantity = Math.Max(0m, product.MinQuantity);

        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<bool> IsProductUsedInDocumentsAsync(int productId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StockDocumentItems.AnyAsync(i => i.ProductId == productId);
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteProductAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var product = await context.Products
            .Include(p => p.StockBalance)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return (false, "Товар не знайдено.");

        bool usedInDocuments = await context.StockDocumentItems.AnyAsync(i => i.ProductId == id);
        if (usedInDocuments)
            return (false, "Неможливо видалити товар, який вже використовується в складських накладних.");

        if (product.StockBalance != null && product.StockBalance.CurrentQuantity > 0)
            return (false, "Неможливо видалити товар із наявним залишком на складі. Спочатку проведіть списання.");

        context.Products.Remove(product);
        await context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.StockBalance)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.StockBalance)
            .ToListAsync();

        return list
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<List<Product>> GetLowStockProductsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.StockBalance)
            .Where(p => p.StockBalance != null && p.StockBalance.CurrentQuantity <= p.MinQuantity)
            .OrderBy(p => p.StockBalance!.CurrentQuantity)
            .ToListAsync();
    }

    public async Task<Unit> CreateUnitAsync(Unit unit)
    {
        if (unit == null) throw new ArgumentNullException(nameof(unit));

        string trimmedCode = unit.Code?.Trim() ?? string.Empty;
        string trimmedName = unit.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedCode) || string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Код та назва одиниці виміру не можуть бути порожніми.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingCodes = await context.Units
            .AsNoTracking()
            .Select(u => u.Code)
            .ToListAsync();

        if (existingCodes.Any(c => string.Equals(c.Trim(), trimmedCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Одиниця виміру з кодом «{trimmedCode}» вже зареєстрована.");

        var entity = new Unit
        {
            Code = trimmedCode,
            Name = trimmedName,
            IsFractional = unit.IsFractional
        };

        context.Units.Add(entity);
        await context.SaveChangesAsync();

        unit.Id = entity.Id;
        unit.Code = entity.Code;
        unit.Name = entity.Name;
        return entity;
    }

    public async Task UpdateUnitAsync(Unit unit)
    {
        if (unit == null) throw new ArgumentNullException(nameof(unit));

        string trimmedCode = unit.Code?.Trim() ?? string.Empty;
        string trimmedName = unit.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedCode) || string.IsNullOrWhiteSpace(trimmedName))
            throw new InvalidOperationException("Код та назва одиниці виміру не можуть бути порожніми.");

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existing = await context.Units.FindAsync(unit.Id);
        if (existing == null)
            throw new KeyNotFoundException($"Одиницю виміру з ID {unit.Id} не знайдено.");

        var otherCodes = await context.Units
            .AsNoTracking()
            .Where(u => u.Id != unit.Id)
            .Select(u => u.Code)
            .ToListAsync();

        if (otherCodes.Any(c => string.Equals(c.Trim(), trimmedCode, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Одиниця виміру з кодом «{trimmedCode}» вже зареєстрована.");

        existing.Code = trimmedCode;
        existing.Name = trimmedName;
        existing.IsFractional = unit.IsFractional;

        await context.SaveChangesAsync();
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteUnitAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var unit = await context.Units.FindAsync(id);
        if (unit == null)
            return (false, "Одиницю виміру не знайдено.");

        bool hasProducts = await context.Products.AnyAsync(p => p.UnitId == id);
        if (hasProducts)
            return (false, "Неможливо видалити одиницю виміру, яка призначена товарам.");

        context.Units.Remove(unit);
        await context.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<List<Unit>> GetAllUnitsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var list = await context.Units
            .AsNoTracking()
            .ToListAsync();

        return list
            .OrderBy(u => u.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<string> GenerateNextSkuAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var skus = await context.Products
            .Select(p => p.Sku)
            .Where(s => s.StartsWith("ART-"))
            .ToListAsync();

        int maxNumber = 0;
        foreach (var sku in skus)
        {
            var part = sku.Replace("ART-", string.Empty);
            if (int.TryParse(part, out int num) && num > maxNumber)
            {
                maxNumber = num;
            }
        }

        return $"ART-{(maxNumber + 1):D5}";
    }
}