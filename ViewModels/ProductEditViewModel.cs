using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class EditProductViewModel : ObservableObject
{
    private readonly Product _product;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;

    private bool _isInitializing = true;
    private Unit? _initialUnit;
    private Unit? _previousSelectedUnit;
    private decimal _initialStockQuantity;

    [ObservableProperty]
    private string _sku = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isUsedInDocuments;

    [ObservableProperty]
    private bool _isNameEditable = true;

    [ObservableProperty]
    private string _stockBalanceDisplay = "Залишок: 0";

    [ObservableProperty]
    private string _unitCategoryNotice = "В рамках поточної категорії виміру";

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<Unit> _compatibleUnits = new();

    [ObservableProperty]
    private Unit? _selectedUnit;

    [ObservableProperty]
    private string _purchasePriceText = "0.00";

    [ObservableProperty]
    private string _retailPriceText = "0.00";

    [ObservableProperty]
    private string _minQuantityText = "5";

    public Product ResultProduct => _product;

    public EditProductViewModel(
        Product product,
        IProductService productService,
        IDialogService dialogService)
    {
        _product = product;
        _productService = productService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        _isInitializing = true;
        try
        {
            var productTask = _productService.GetProductByIdAsync(_product.Id);
            var isUsedTask = _productService.IsProductUsedInDocumentsAsync(_product.Id);
            var categoriesTask = _productService.GetAllCategoriesAsync();
            var unitsTask = _productService.GetAllUnitsAsync();

            await Task.WhenAll(productTask, isUsedTask, categoriesTask, unitsTask);

            var productToUse = await productTask ?? _product;
            IsUsedInDocuments = await isUsedTask;
            IsNameEditable = !IsUsedInDocuments;

            var allCategories = await categoriesTask;
            var allUnits = await unitsTask;

            Sku = productToUse.Sku;
            Name = productToUse.Name;
            _initialStockQuantity = productToUse.StockBalance?.CurrentQuantity ?? 0m;

            Categories = new ObservableCollection<Category>(allCategories);
            SelectedCategory = Categories.FirstOrDefault(c => c.Id == productToUse.CategoryId);

            _initialUnit = productToUse.Unit ?? allUnits.FirstOrDefault(u => u.Id == productToUse.UnitId);
            _previousSelectedUnit = _initialUnit;

            var unitCategory = UnitCategoryHelper.GetCategory(_initialUnit);
            string catDisplayName = UnitCategoryHelper.GetCategoryDisplayName(unitCategory);
            UnitCategoryNotice = $"Категорія: {catDisplayName}";

            var filteredUnits = allUnits
                .Where(u => UnitCategoryHelper.GetCategory(u) == unitCategory)
                .ToList();

            if (_initialUnit != null && !filteredUnits.Any(u => u.Id == _initialUnit.Id))
            {
                filteredUnits.Insert(0, _initialUnit);
            }

            CompatibleUnits = new ObservableCollection<Unit>(filteredUnits);
            SelectedUnit = CompatibleUnits.FirstOrDefault(u => u.Id == productToUse.UnitId)
                        ?? CompatibleUnits.FirstOrDefault();

            PurchasePriceText = NumericParserHelper.FormatPrice(productToUse.PurchasePrice);
            RetailPriceText = NumericParserHelper.FormatPrice(productToUse.RetailPrice);
            MinQuantityText = NumericParserHelper.FormatQuantity(productToUse.MinQuantity);

            UpdateStockBalancePreview(SelectedUnit);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка завантаження", $"Не вдалося підготувати дані товару: {ex.Message}");
        }
        finally
        {
            _isInitializing = false;
        }
    }

    partial void OnSelectedUnitChanged(Unit? value)
    {
        if (_isInitializing || value == null || _previousSelectedUnit == null)
        {
            _previousSelectedUnit = value;
            return;
        }

        if (_previousSelectedUnit.Id != value.Id && UnitCategoryHelper.AreCompatible(_previousSelectedUnit, value))
        {
            if (NumericParserHelper.TryParseNonNegativeDecimal(MinQuantityText, out decimal currentMin))
            {
                decimal convertedMin = UnitCategoryHelper.ConvertQuantity(currentMin, _previousSelectedUnit, value);
                MinQuantityText = NumericParserHelper.FormatQuantity(convertedMin);
            }

            if (NumericParserHelper.TryParseNonNegativeDecimal(PurchasePriceText, out decimal currentPurchase))
            {
                decimal convertedPurchase = UnitCategoryHelper.ConvertUnitPrice(currentPurchase, _previousSelectedUnit, value);
                PurchasePriceText = NumericParserHelper.FormatPrice(convertedPurchase);
            }

            if (NumericParserHelper.TryParseNonNegativeDecimal(RetailPriceText, out decimal currentRetail))
            {
                decimal convertedRetail = UnitCategoryHelper.ConvertUnitPrice(currentRetail, _previousSelectedUnit, value);
                RetailPriceText = NumericParserHelper.FormatPrice(convertedRetail);
            }

            UpdateStockBalancePreview(value);
        }

        _previousSelectedUnit = value;
    }

    private void UpdateStockBalancePreview(Unit? currentUnit)
    {
        decimal previewQty = _initialStockQuantity;
        if (_initialUnit != null && currentUnit != null && _initialUnit.Id != currentUnit.Id &&
            UnitCategoryHelper.AreCompatible(_initialUnit, currentUnit))
        {
            previewQty = UnitCategoryHelper.ConvertQuantity(_initialStockQuantity, _initialUnit, currentUnit);
        }

        StockBalanceDisplay = $"Залишок: {NumericParserHelper.FormatQuantity(previewQty)} {currentUnit?.Code}";
    }

    [RelayCommand]
    private async Task SaveAsync(Window? window)
    {
        string trimmedName = Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            await _dialogService.ShowInfoAsync("Увага", "Назва товару не може бути порожньою.");
            return;
        }

        if (IsUsedInDocuments && !string.Equals(trimmedName, _product.Name, StringComparison.OrdinalIgnoreCase))
        {
            await _dialogService.ShowErrorAsync(
                "Редагування заборонено",
                "Неможливо змінити назву товару, оскільки цей товар уже було використано в складських накладних.");
            return;
        }

        if (SelectedCategory == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть категорію товару.");
            return;
        }

        if (SelectedUnit == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть одиницю виміру.");
            return;
        }

        if (_initialUnit != null && !UnitCategoryHelper.AreCompatible(_initialUnit, SelectedUnit))
        {
            var oldCat = UnitCategoryHelper.GetCategory(_initialUnit);
            var newCat = UnitCategoryHelper.GetCategory(SelectedUnit);
            await _dialogService.ShowErrorAsync(
                "Неприпустима одиниця виміру",
                $"Неможливо перевести «{_initialUnit.Name}» ({UnitCategoryHelper.GetCategoryDisplayName(oldCat)}) в «{SelectedUnit.Name}» ({UnitCategoryHelper.GetCategoryDisplayName(newCat)}).\nОдиницю виміру можна змінювати лише в рамках однієї категорії вимірювання (наприклад, не можна перевести сантиметри в штуки).");
            return;
        }

        if (!NumericParserHelper.TryParseNonNegativeDecimal(MinQuantityText, out decimal minQty))
        {
            await _dialogService.ShowInfoAsync("Увага", "Вкажіть коректний невід'ємний мінімальний поріг залишку.");
            return;
        }

        if (!NumericParserHelper.TryParseNonNegativeDecimal(PurchasePriceText, out decimal purchasePrice))
        {
            await _dialogService.ShowInfoAsync("Увага", "Вкажіть коректну невід'ємну закупівельну ціну.");
            return;
        }

        if (!NumericParserHelper.TryParseNonNegativeDecimal(RetailPriceText, out decimal retailPrice))
        {
            await _dialogService.ShowInfoAsync("Увага", "Вкажіть коректну невід'ємну роздрібну ціну.");
            return;
        }

        try
        {
            _product.Name = trimmedName;
            _product.CategoryId = SelectedCategory.Id;
            _product.Category = SelectedCategory;
            _product.UnitId = SelectedUnit.Id;
            _product.Unit = SelectedUnit;
            _product.PurchasePrice = purchasePrice;
            _product.RetailPrice = retailPrice;
            _product.MinQuantity = minQty;

            await _productService.UpdateProductAsync(_product);
            WeakReferenceMessenger.Default.Send(new ProductsChangedMessage());

            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка збереження", ex.Message);
        }
    }

    [RelayCommand]
    private void Cancel(Window? window)
    {
        if (window != null)
        {
            window.DialogResult = false;
            window.Close();
        }
    }
}