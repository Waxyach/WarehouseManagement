using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.ViewModels;
using WarehouseManagementApp.Views;

namespace WarehouseManagementApp.Services;

public class WindowService : IWindowService
{
    private readonly IServiceProvider _serviceProvider;

    public WindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    private static Window? GetActiveWindow() =>
        Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
        ?? Application.Current?.MainWindow;

    public bool? ShowCategoriesWindow()
    {
        var window = _serviceProvider.GetRequiredService<CategoriesManagementWindow>();
        window.Owner = GetActiveWindow();
        return window.ShowDialog();
    }

    public Category? ShowCategoryEditorWindow(Category? existingCategory = null)
    {
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var vm = new CategoryViewModel(dialogService, existingCategory);
        var window = new CategoryWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog() == true ? window.ResultCategory : null;
    }

    public bool? ShowUnitsWindow()
    {
        var window = _serviceProvider.GetRequiredService<UnitsManagementWindow>();
        window.Owner = GetActiveWindow();
        return window.ShowDialog();
    }

    public Unit? ShowUnitEditorWindow(Unit? existingUnit = null)
    {
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var vm = new UnitEditViewModel(dialogService, existingUnit);
        var window = new UnitEditWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog() == true ? window.ResultUnit : null;
    }

    public bool? ShowCreateProductWindow()
    {
        var vm = _serviceProvider.GetRequiredService<CreateProductViewModel>();
        var window = new CreateProductWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog();
    }

    public bool? ShowEditProductWindow(Product product)
    {
        var productService = _serviceProvider.GetRequiredService<IProductService>();
        var dialogService = _serviceProvider.GetRequiredService<IDialogService>();
        var vm = new EditProductViewModel(product, productService, dialogService);
        var window = new EditProductWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog();
    }

    public bool? ShowCreateDocumentWindow()
    {
        var vm = _serviceProvider.GetRequiredService<CreateDocumentViewModel>();
        var window = new CreateDocumentWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog();
    }

    public bool? ShowDocumentDetailsWindow(StockDocument document)
    {
        var vm = new DocumentDetailsViewModel(document);
        var window = new DocumentDetailsWindow(vm)
        {
            Owner = GetActiveWindow()
        };
        return window.ShowDialog();
    }
}