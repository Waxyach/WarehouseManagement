using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public interface IDocumentService
{
    Task<List<StockDocument>> GetAllDocumentsAsync();
    Task<StockDocument?> GetDocumentWithDetailsAsync(int documentId);
    Task ProcessDocumentAsync(StockDocument document);
    Task<string> GenerateNextDocumentNumberAsync(DocumentType type);
}