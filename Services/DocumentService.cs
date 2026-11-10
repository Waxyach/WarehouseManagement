using Microsoft.EntityFrameworkCore;
using WarehouseManagementApp.Data;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<WarehouseDbContext> _contextFactory;

    public DocumentService(IDbContextFactory<WarehouseDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<StockDocument>> GetAllDocumentsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StockDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(d => d.Items)
                .ThenInclude(i => i.Product!)
                    .ThenInclude(p => p.Unit)
            .OrderByDescending(d => d.Date)
            .ToListAsync();
    }

    public async Task<StockDocument?> GetDocumentWithDetailsAsync(int documentId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.StockDocuments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(d => d.Items)
                .ThenInclude(i => i.Product!)
                    .ThenInclude(p => p.Unit)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task ProcessDocumentAsync(StockDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (document.Items == null || document.Items.Count == 0)
            throw new InvalidOperationException("Неможливо провести накладну без жодної позиції.");

        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var productIds = document.Items
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            // Batch-load all products with units and stock balances in a single SQL query (fixes N+1 problem)
            var productsMap = await context.Products
                .Include(p => p.Unit)
                .Include(p => p.StockBalance)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Group quantities by ProductId to accurately validate total requested quantity per product
            var aggregatedQuantities = document.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

            foreach (var (productId, totalQuantity) in aggregatedQuantities)
            {
                if (totalQuantity <= 0m)
                    throw new InvalidOperationException("Кількість товару в накладній повинна бути більшою за нуль.");

                if (!productsMap.TryGetValue(productId, out var product))
                    throw new KeyNotFoundException($"Товар з ID {productId} не знайдено в базі даних.");

                if (product.Unit != null && !product.Unit.IsFractional && totalQuantity % 1 != 0)
                {
                    throw new InvalidOperationException(
                        $"Товар «{product.Name}» має цілочисельну одиницю виміру ({product.Unit.Code}) і не підтримує дробову кількість ({totalQuantity:0.###}).");
                }

                if (document.Type is DocumentType.Expense or DocumentType.WriteOff)
                {
                    decimal currentQty = product.StockBalance?.CurrentQuantity ?? 0m;
                    if (currentQty < totalQuantity)
                    {
                        string unitCode = product.Unit?.Code ?? "од.";
                        throw new InvalidOperationException(
                            $"Недостатньо товару «{product.Name}» на складі. Доступно: {currentQty:0.###} {unitCode}, потрібно: {totalQuantity:0.###} {unitCode}.");
                    }
                }
            }

            // Prepare clean document entity without navigation graph conflicts
            var docEntity = new StockDocument
            {
                DocumentNumber = string.IsNullOrWhiteSpace(document.DocumentNumber)
                    ? await GenerateNextDocumentNumberInternalAsync(context, document.Type, document.Date)
                    : document.DocumentNumber.Trim(),
                Date = document.Date == default ? DateTime.Now : document.Date,
                Type = document.Type,
                Counterparty = document.Counterparty?.Trim(),
                Note = document.Note?.Trim(),
                TotalAmount = Math.Round(document.Items.Sum(i => i.Quantity * i.Price), 2),
                Items = document.Items.Select(i => new StockDocumentItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            context.StockDocuments.Add(docEntity);

            var nowUtc = DateTime.UtcNow;
            foreach (var (productId, totalQuantity) in aggregatedQuantities)
            {
                var product = productsMap[productId];
                var balance = product.StockBalance;

                if (balance == null)
                {
                    balance = new StockBalance
                    {
                        ProductId = productId,
                        CurrentQuantity = 0m,
                        LastUpdated = nowUtc
                    };
                    context.StockBalances.Add(balance);
                    product.StockBalance = balance;
                }

                if (document.Type == DocumentType.Receipt)
                {
                    balance.CurrentQuantity += totalQuantity;
                }
                else if (document.Type is DocumentType.Expense or DocumentType.WriteOff)
                {
                    balance.CurrentQuantity -= totalQuantity;
                }

                balance.LastUpdated = nowUtc;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            document.Id = docEntity.Id;
            document.DocumentNumber = docEntity.DocumentNumber;
            document.TotalAmount = docEntity.TotalAmount;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<string> GenerateNextDocumentNumberAsync(DocumentType type)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await GenerateNextDocumentNumberInternalAsync(context, type, DateTime.Today);
    }

    private static async Task<string> GenerateNextDocumentNumberInternalAsync(
        WarehouseDbContext context,
        DocumentType type,
        DateTime date)
    {
        string prefix = DocumentNumberHelper.GetPrefix(type);
        string prefixWithDate = $"{prefix}-{date:yyyyMMdd}-";

        var existingNumbers = await context.StockDocuments
            .AsNoTracking()
            .Where(d => d.DocumentNumber.StartsWith(prefixWithDate))
            .Select(d => d.DocumentNumber)
            .ToListAsync();

        int maxSeq = existingNumbers
            .Select(num => DocumentNumberHelper.ExtractSequenceNumber(num, prefixWithDate))
            .DefaultIfEmpty(0)
            .Max();

        return DocumentNumberHelper.FormatDocumentNumber(type, date, maxSeq + 1);
    }
}