namespace WarehouseManagementApp.Services;

public interface IReportService
{
    Task ExportStockReportToExcelAsync(string filePath);
}