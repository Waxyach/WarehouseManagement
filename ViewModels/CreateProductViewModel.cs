using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class CreateProductViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;

    [ObservableProperty]
    private string _sku = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<Unit> _units = new();

    [ObservableProperty]
    private Unit? _selectedUnit;

    [ObservableProperty]
    private string _purchasePriceText = "0.00";

    [ObservableProperty]
    private string _retailPriceText = "0.00";

    [ObservableProperty]
    private string _minQuantityText = "5";

    public Product? ResultProduct { get; private set; }

    public CreateProductViewModel(IProductService productService, IDialogService dialogService)
    {
        _productService = productService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            var categoriesTask = _productService.GetAllCategoriesAsync();
            var unitsTask = _productService.GetAllUnitsAsync();

            await Task.WhenAll(categoriesTask, unitsTask);

            Categories = new ObservableCollection<Category>(await categoriesTask);
            Units = new ObservableCollection<Unit>(await unitsTask);

            if (Categories.Count > 0) SelectedCategory = Categories[0];
            if (Units.Count > 0) SelectedUnit = Units[0];

            await GenerateSkuAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка завантаження", $"Не вдалося завантажити довідники: {ex.Message}");
        }
    }

    private async Task GenerateSkuAsync()
    {
        try
        {
            var nextSku = await _productService.GenerateNextSkuAsync();
            if (!string.IsNullOrWhiteSpace(nextSku))
            {
                Sku = nextSku;
                return;
            }
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task SaveAsync(Window? window)
    {
        string trimmedSku = Sku?.Trim() ?? string.Empty;
        string trimmedName = Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedSku) || string.IsNullOrWhiteSpace(trimmedName))
        {
            await _dialogService.ShowInfoAsync("Увага", "Заповніть назву товару та артикул (SKU).");
            return;
        }

        if (SelectedCategory == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть категорію товару (спочатку створіть категорію, якщо список порожній).");
            return;
        }

        if (SelectedUnit == null)
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть одиницю виміру.");
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

        if (!NumericParserHelper.TryParseNonNegativeDecimal(MinQuantityText, out decimal minQty))
        {
            await _dialogService.ShowInfoAsync("Увага", "Вкажіть коректний невід'ємний мінімальний поріг залишку.");
            return;
        }

        try
        {
            var newProduct = new Product
            {
                Sku = trimmedSku,
                Name = trimmedName,
                CategoryId = SelectedCategory.Id,
                UnitId = SelectedUnit.Id,
                PurchasePrice = purchasePrice,
                RetailPrice = retailPrice,
                MinQuantity = minQty
            };

            ResultProduct = await _productService.CreateProductAsync(newProduct);
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

