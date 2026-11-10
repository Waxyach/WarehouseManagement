using System.IO;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using WarehouseManagementApp.Data;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public class ReportService : IReportService
{
    private readonly IDbContextFactory<WarehouseDbContext> _contextFactory;

    public ReportService(IDbContextFactory<WarehouseDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task ExportStockReportToExcelAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Шлях до файлу не може бути порожнім.", nameof(filePath));
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var rawProducts = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .Include(p => p.StockBalance)
            .ToListAsync();

        var products = rawProducts
            .OrderBy(p => p.Category?.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        try
        {
            await Task.Run(() => GenerateAndSaveWorkbook(products, filePath));
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Файл «{Path.GetFileName(filePath)}» зайнятий іншим процесом (наприклад, відкритий у Microsoft Excel). Закрийте його та спробуйте знову.",
                ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Не вдалося зберегти Excel-файл: {ex.Message}", ex);
        }
    }

    private static void GenerateAndSaveWorkbook(List<Product> products, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Залишки");

        ws.Style.Font.FontName = "Segoe UI";
        ws.Style.Font.FontSize = 10;
        ws.ShowGridLines = true;

        ws.Cell("A1").Value = "ЗВІТ ПРО ЗАЛИШКИ ТОВАРІВ НА СКЛАДІ";
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#0F172A");

        ws.Cell("A2").Value = $"Дата формування: {DateTime.Now:dd.MM.yyyy HH:mm}";
        ws.Cell("A2").Style.Font.FontSize = 10;
        ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#64748B");

        int totalPositions = products.Count;
        int deficitCount = products.Count(p => (p.StockBalance?.CurrentQuantity ?? 0m) <= p.MinQuantity);

        ws.Range("A4:B4").Merge().Value = "Всього позицій";
        ws.Range("A4:B4").Style.Font.FontSize = 9;
        ws.Range("A4:B4").Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Range("A5:B5").Merge().Value = totalPositions;
        ws.Range("A5:B5").Style.Font.FontSize = 14;
        ws.Range("A5:B5").Style.Font.Bold = true;
        ws.Range("A4:B5").Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
        ws.Range("A4:B5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range("A4:B5").Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");

        ws.Range("C4:D4").Merge().Value = "Товари в дефіциті";
        ws.Range("C4:D4").Style.Font.FontSize = 9;
        ws.Range("C4:D4").Style.Font.FontColor = XLColor.FromHtml("#64748B");
        ws.Range("C5:D5").Merge().Value = deficitCount;
        ws.Range("C5:D5").Style.Font.FontSize = 14;
        ws.Range("C5:D5").Style.Font.Bold = true;
        ws.Range("C5:D5").Style.Font.FontColor = deficitCount > 0 ? XLColor.FromHtml("#DC2626") : XLColor.FromHtml("#16A34A");
        ws.Range("C4:D5").Style.Fill.BackgroundColor = deficitCount > 0 ? XLColor.FromHtml("#FEF2F2") : XLColor.FromHtml("#F0FDF4");
        ws.Range("C4:D5").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range("C4:D5").Style.Border.OutsideBorderColor = deficitCount > 0 ? XLColor.FromHtml("#FECACA") : XLColor.FromHtml("#BBF7D0");

        int startRow = 7;
        string[] headers =
        {
            "№", "Артикул", "Назва товару", "Категорія", "Од. вим.",
            "Поточний залишок", "Мін. поріг", "Закупівельна ціна",
            "Сума залишку", "Статус"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(startRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.Horizontal = i is 0 or 4 or 9
                ? XLAlignmentHorizontalValues.Center
                : (i >= 5 ? XLAlignmentHorizontalValues.Right : XLAlignmentHorizontalValues.Left);
        }
        ws.Row(startRow).Height = 26;

        int currentRow = startRow + 1;
        int itemIndex = 1;

        foreach (var p in products)
        {
            decimal currentQty = p.StockBalance?.CurrentQuantity ?? 0m;
            bool isDeficit = currentQty <= p.MinQuantity;

            ws.Row(currentRow).Height = 22;

            ws.Cell(currentRow, 1).Value = itemIndex;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#64748B");

            ws.Cell(currentRow, 2).Value = p.Sku;
            ws.Cell(currentRow, 2).Style.Font.Bold = true;

            ws.Cell(currentRow, 3).Value = p.Name;

            ws.Cell(currentRow, 4).Value = p.Category?.Name ?? "—";
            ws.Cell(currentRow, 5).Value = p.Unit?.Code ?? "";
            ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(currentRow, 6).Value = currentQty;
            ws.Cell(currentRow, 6).Style.NumberFormat.Format = "#,##0.###";

            ws.Cell(currentRow, 7).Value = p.MinQuantity;
            ws.Cell(currentRow, 7).Style.NumberFormat.Format = "#,##0.###";

            ws.Cell(currentRow, 8).Value = p.PurchasePrice;
            ws.Cell(currentRow, 8).Style.NumberFormat.Format = "#,##0.00 \"грн\"";

            ws.Cell(currentRow, 9).FormulaA1 = $"=F{currentRow}*H{currentRow}";
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00 \"грн\"";
            ws.Cell(currentRow, 9).Style.Font.Bold = true;

            var statusCell = ws.Cell(currentRow, 10);
            statusCell.Value = isDeficit ? "ДЕФІЦИТ" : "В наявності";
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Font.FontSize = 9;

            if (isDeficit)
            {
                ws.Row(currentRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF1F2");
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#BE123C");
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE4E6");
            }
            else
            {
                if (itemIndex % 2 == 0)
                {
                    ws.Row(currentRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }
                statusCell.Style.Font.FontColor = XLColor.FromHtml("#15803D");
                statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");
            }

            var rowRange = ws.Range(currentRow, 1, currentRow, headers.Length);
            rowRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            rowRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#E2E8F0");
            rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            itemIndex++;
            currentRow++;
        }

        if (products.Count > 0)
        {
            ws.Row(currentRow).Height = 26;
            ws.Cell(currentRow, 1).Value = "РАЗОМ:";
            ws.Range(currentRow, 1, currentRow, 8).Merge();
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            ws.Cell(currentRow, 1).Style.Font.Bold = true;

            ws.Cell(currentRow, 9).FormulaA1 = $"=SUM(I{startRow + 1}:I{currentRow - 1})";
            ws.Cell(currentRow, 9).Style.NumberFormat.Format = "#,##0.00 \"грн\"";
            ws.Cell(currentRow, 9).Style.Font.Bold = true;
            ws.Cell(currentRow, 9).Style.Font.FontSize = 11;

            var totalRange = ws.Range(currentRow, 1, currentRow, headers.Length);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            totalRange.Style.Border.TopBorder = XLBorderStyleValues.Medium;
            totalRange.Style.Border.TopBorderColor = XLColor.FromHtml("#94A3B8");
            totalRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            totalRange.Style.Border.BottomBorderColor = XLColor.FromHtml("#475569");
            totalRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        ws.Columns().AdjustToContents();
        foreach (var col in ws.Columns(1, headers.Length))
        {
            col.Width += 3;
        }

        ws.Range(startRow, 1, Math.Max(currentRow - 1, startRow), headers.Length).SetAutoFilter();
        ws.SheetView.FreezeRows(startRow);

        workbook.SaveAs(filePath);
    }
}