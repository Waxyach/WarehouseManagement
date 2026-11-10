using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public interface IProductService
{
    Task<Category> CreateCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task<(bool Success, string ErrorMessage)> DeleteCategoryAsync(int id);
    Task<List<Category>> GetAllCategoriesAsync();
    Task<Product> CreateProductAsync(Product product);
    Task UpdateProductAsync(Product product);
    Task<(bool Success, string ErrorMessage)> DeleteProductAsync(int id);
    Task<Product?> GetProductByIdAsync(int id);
    Task<List<Product>> GetAllProductsAsync();
    Task<List<Product>> GetLowStockProductsAsync();
    Task<bool> IsProductUsedInDocumentsAsync(int productId);
    Task<Unit> CreateUnitAsync(Unit unit);
    Task UpdateUnitAsync(Unit unit);
    Task<(bool Success, string ErrorMessage)> DeleteUnitAsync(int id);
    Task<List<Unit>> GetAllUnitsAsync();
    Task<string> GenerateNextSkuAsync();
}