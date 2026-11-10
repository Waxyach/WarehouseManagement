using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class ProductsViewModel : ObservableObject,
    IRecipient<DocumentsChangedMessage>,
    IRecipient<DictionariesChangedMessage>
{
    private readonly IProductService _productService;
    private readonly IReportService _reportService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;

    private List<Product> _allProducts = new();
    private bool _isSuppressingFilter;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private ObservableCollection<CategoryGroupItemViewModel> _categoryGroups = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<Unit> _units = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategoryFilter;

    [ObservableProperty]
    private bool _onlyLowStock;

    [ObservableProperty]
    private bool _isFolderView;

    [ObservableProperty]
    private bool _isLoading;

    public ProductsViewModel(
        IProductService productService,
        IReportService reportService,
        IDialogService dialogService,
        IWindowService windowService)
    {
        _productService = productService;
        _reportService = reportService;
        _dialogService = dialogService;
        _windowService = windowService;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(DocumentsChangedMessage message) => _ = LoadDataAsync();

    public void Receive(DictionariesChangedMessage message) => _ = LoadDataAsync();

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            int? previousSelectedProductId = SelectedProduct?.Id;
            int? previousCategoryFilterId = SelectedCategoryFilter?.Id;

            var productsTask = _productService.GetAllProductsAsync();
            var categoriesTask = _productService.GetAllCategoriesAsync();
            var unitsTask = _productService.GetAllUnitsAsync();

            await Task.WhenAll(productsTask, categoriesTask, unitsTask);

            _allProducts = await productsTask;

            var dbCategories = await categoriesTask;
            var categoriesWithAll = new List<Category>(dbCategories.Count + 1)
            {
                new() { Id = 0, Name = "Всі категорії", ColorHex = "#9CA3AF" }
            };
            categoriesWithAll.AddRange(dbCategories);

            _isSuppressingFilter = true;
            try
            {
                Categories = new ObservableCollection<Category>(categoriesWithAll);
                SelectedCategoryFilter = Categories.FirstOrDefault(c => c.Id == previousCategoryFilterId)
                                      ?? Categories[0];
                Units = new ObservableCollection<Unit>(await unitsTask);
            }
            finally
            {
                _isSuppressingFilter = false;
            }

            ApplyFilter(previousSelectedProductId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка завантаження", $"Не вдалося завантажити дані: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateProductAsync()
    {
        if (_windowService.ShowCreateProductWindow() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task EditProductAsync(Product? productToEdit = null)
    {
        var target = productToEdit ?? SelectedProduct;
        if (target == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть товар у списку для редагування.");
            return;
        }

        if (_windowService.ShowEditProductWindow(target) == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task OpenCategoriesAsync()
    {
        if (_windowService.ShowCategoriesWindow() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private async Task OpenUnitsAsync()
    {
        if (_windowService.ShowUnitsWindow() == true)
        {
            await LoadDataAsync();
        }
    }

    [RelayCommand]
    private void SetTableView() => IsFolderView = false;

    [RelayCommand]
    private void SetFolderView() => IsFolderView = true;

    partial void OnSearchFilterChanged(string value)
    {
        if (!_isSuppressingFilter) ApplyFilter();
    }

    partial void OnSelectedCategoryFilterChanged(Category? value)
    {
        if (!_isSuppressingFilter) ApplyFilter();
    }

    partial void OnOnlyLowStockChanged(bool value)
    {
        if (!_isSuppressingFilter) ApplyFilter();
    }

    partial void OnIsFolderViewChanged(bool value)
    {
        if (!_isSuppressingFilter) ApplyFilter();
    }

    private void ApplyFilter(int? restoreSelectedProductId = null)
    {
        int? targetProductId = restoreSelectedProductId ?? SelectedProduct?.Id;

        var expandedCategoryIds = CategoryGroups
            .Where(g => g.IsExpanded)
            .Select(g => g.Category.Id)
            .ToHashSet();

        IEnumerable<Product> filtered = _allProducts;

        if (SelectedCategoryFilter != null && SelectedCategoryFilter.Id != 0)
        {
            filtered = filtered.Where(p => p.CategoryId == SelectedCategoryFilter.Id);
        }

        if (!string.IsNullOrWhiteSpace(SearchFilter))
        {
            string query = SearchFilter.Trim();
            filtered = filtered.Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        if (OnlyLowStock)
        {
            filtered = filtered.Where(p => (p.StockBalance?.CurrentQuantity ?? 0m) <= p.MinQuantity);
        }

        var resultList = filtered
            .OrderBy(p => p.Category?.Name ?? "—", StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Products = new ObservableCollection<Product>(resultList);

        if (targetProductId.HasValue)
        {
            SelectedProduct = Products.FirstOrDefault(p => p.Id == targetProductId.Value);
        }

        var groups = resultList
            .GroupBy(p => p.CategoryId)
            .Select(g =>
            {
                var category = g.First().Category ?? new Category { Id = g.Key, Name = "Без категорії", ColorHex = "#757575" };
                return new CategoryGroupItemViewModel(category, g.ToList())
                {
                    IsExpanded = expandedCategoryIds.Contains(category.Id)
                };
            })
            .OrderBy(g => g.Category.Name, StringComparer.CurrentCultureIgnoreCase);

        CategoryGroups = new ObservableCollection<CategoryGroupItemViewModel>(groups);
    }

    [RelayCommand]
    public async Task DeleteProductAsync()
    {
        if (SelectedProduct == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть товар у списку для видалення.");
            return;
        }

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Підтвердження видалення",
            $"Ви дійсно бажаєте видалити товар «{SelectedProduct.Name}»?",
            "Видалити",
            "Скасувати");

        if (!confirmed) return;

        var (success, error) = await _productService.DeleteProductAsync(SelectedProduct.Id);
        if (success)
        {
            WeakReferenceMessenger.Default.Send(new ProductsChangedMessage());
            await LoadDataAsync();
        }
        else
        {
            await _dialogService.ShowErrorAsync("Неможливо видалити", error);
        }
    }

    [RelayCommand]
    public async Task ExportToExcelAsync()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Таблиці Excel (*.xlsx)|*.xlsx",
            FileName = $"Звіт_залишків_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _reportService.ExportStockReportToExcelAsync(dialog.FileName);
                await _dialogService.ShowInfoAsync("Експорт завершено", "Звіт про залишки успішно збережено у вибраний файл.");
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка експорту", ex.Message);
            }
        }
    }
}