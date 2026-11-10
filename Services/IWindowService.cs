using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Services;

public interface IWindowService
{
    bool? ShowCategoriesWindow();
    Category? ShowCategoryEditorWindow(Category? existingCategory = null);
    bool? ShowUnitsWindow();
    Unit? ShowUnitEditorWindow(Unit? existingUnit = null);
    bool? ShowCreateProductWindow();
    bool? ShowEditProductWindow(Product product);
    bool? ShowCreateDocumentWindow();
    bool? ShowDocumentDetailsWindow(StockDocument document);
}