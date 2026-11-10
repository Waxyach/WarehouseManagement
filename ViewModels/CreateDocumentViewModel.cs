using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public record DocumentTypeItem(DocumentType Type, string DisplayName);

public partial class CreateDocumentViewModel : ObservableObject
{
    private readonly IDocumentService _documentService;
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;

    public ObservableCollection<DocumentType> DocumentTypes { get; } = new()
    {
        DocumentType.Receipt,
        DocumentType.Expense,
        DocumentType.WriteOff
    };

    [ObservableProperty]
    private DocumentType _selectedType = DocumentType.Receipt;

    [ObservableProperty]
    private string _documentNumber = string.Empty;

    [ObservableProperty]
    private string _counterparty = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Product> _availableProducts = new();

    [ObservableProperty]
    private ObservableCollection<StockDocumentItem> _items = new();

    [ObservableProperty]
    private StockDocumentItem? _selectedItem;

    [ObservableProperty]
    private Product? _selectedProductToAdd;

    [ObservableProperty]
    private decimal _quantityToAdd = 1;

    [ObservableProperty]
    private decimal _maxAvailableQuantity = decimal.MaxValue;

    [ObservableProperty]
    private bool _isStockLimitActive;

    [ObservableProperty]
    private bool _isQuantityExceeded;

    [ObservableProperty]
    private string _stockHintText = string.Empty;

    [ObservableProperty]
    private bool _canIncreaseQuantity = true;

    [ObservableProperty]
    private decimal _priceToAdd;

    [ObservableProperty]
    private decimal _totalAmount;

    public CreateDocumentViewModel(
        IDocumentService documentService,
        IProductService productService,
        IDialogService dialogService,
        IWindowService windowService)
    {
        _documentService = documentService;
        _productService = productService;
        _dialogService = dialogService;
        _windowService = windowService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        Items.Clear();
        Counterparty = string.Empty;
        Note = string.Empty;
        TotalAmount = 0m;

        var products = await _productService.GetAllProductsAsync();
        AvailableProducts = new ObservableCollection<Product>(products);

        if (AvailableProducts.Count > 0)
        {
            SelectedProductToAdd = AvailableProducts[0];
            UpdatePriceForSelectedProduct();
            UpdateStockLimits();
        }
        else
        {
            SelectedProductToAdd = null;
            PriceToAdd = 0m;
        }

        QuantityToAdd = 1;
        await UpdateDocumentNumberAsync();
    }

    partial void OnSelectedTypeChanged(DocumentType value)
    {
        UpdatePriceForSelectedProduct();
        UpdateStockLimits();
        _ = UpdateDocumentNumberAsync();
    }

    partial void OnSelectedProductToAddChanged(Product? value)
    {
        UpdatePriceForSelectedProduct();
        QuantityToAdd = 1;
        UpdateStockLimits();
    }

    partial void OnQuantityToAddChanged(decimal value)
    {
        ValidateQuantityLimit();
    }

    private void UpdateStockLimits()
    {
        if (SelectedProductToAdd == null || SelectedType == DocumentType.Receipt)
        {
            IsStockLimitActive = false;
            IsQuantityExceeded = false;
            MaxAvailableQuantity = decimal.MaxValue;
            StockHintText = string.Empty;
            CanIncreaseQuantity = true;
            return;
        }

        IsStockLimitActive = true;
        decimal stock = SelectedProductToAdd.StockBalance?.CurrentQuantity ?? 0m;
        var alreadyInDoc = Items.FirstOrDefault(i => i.ProductId == SelectedProductToAdd.Id);
        decimal inDoc = alreadyInDoc?.Quantity ?? 0m;

        MaxAvailableQuantity = Math.Max(0m, stock - inDoc);
        ValidateQuantityLimit();
    }

    private void ValidateQuantityLimit()
    {
        if (!IsStockLimitActive)
        {
            IsQuantityExceeded = false;
            CanIncreaseQuantity = true;
            return;
        }

        string unit = SelectedProductToAdd?.Unit?.Code ?? "од.";

        if (QuantityToAdd > MaxAvailableQuantity)
        {
            IsQuantityExceeded = true;
            StockHintText = $"макс: {MaxAvailableQuantity:0.###} {unit}";
            CanIncreaseQuantity = false;
        }
        else
        {
            IsQuantityExceeded = false;
            StockHintText = $"{MaxAvailableQuantity:0.###} {unit}";
            CanIncreaseQuantity = QuantityToAdd < MaxAvailableQuantity;
        }
    }

    private void UpdatePriceForSelectedProduct()
    {
        if (SelectedProductToAdd == null)
        {
            PriceToAdd = 0m;
            return;
        }

        PriceToAdd = SelectedType == DocumentType.Receipt
            ? SelectedProductToAdd.PurchasePrice
            : SelectedProductToAdd.RetailPrice;
    }

    private async Task UpdateDocumentNumberAsync()
    {
        try
        {
            var generatedNumber = await _documentService.GenerateNextDocumentNumberAsync(SelectedType);
            if (!string.IsNullOrWhiteSpace(generatedNumber))
            {
                DocumentNumber = generatedNumber;
                return;
            }
        }
        catch
        {
        }

        DocumentNumber = DocumentNumberHelper.FormatDocumentNumber(SelectedType, DateTime.Today, 1);
    }

    [RelayCommand]
    private async Task QuickCreateProductAsync()
    {
        if (_windowService.ShowCreateProductWindow() == true)
        {
            var products = await _productService.GetAllProductsAsync();
            AvailableProducts = new ObservableCollection<Product>(products);
            if (AvailableProducts.Count > 0)
            {
                SelectedProductToAdd = AvailableProducts.OrderByDescending(p => p.Id).First();
            }
        }
    }

    [RelayCommand]
    public async Task AddItemAsync()
    {
        if (SelectedProductToAdd == null)
        {
            await _dialogService.ShowInfoAsync("Помилка валідації", "Будь ласка, оберіть товар зі списку.");
            return;
        }

        if (QuantityToAdd <= 0)
        {
            await _dialogService.ShowInfoAsync("Помилка валідації", "Кількість товару повинна бути більшою за нуль.");
            return;
        }

        if (SelectedProductToAdd.Unit != null && !SelectedProductToAdd.Unit.IsFractional && QuantityToAdd % 1 != 0)
        {
            await _dialogService.ShowInfoAsync("Помилка одиниці виміру", $"Одиниця «{SelectedProductToAdd.Unit.Name}» не підтримує дробові значення (лише цілі числа).");
            return;
        }

        if (SelectedType is DocumentType.Expense or DocumentType.WriteOff)
        {
            decimal availableStock = SelectedProductToAdd.StockBalance?.CurrentQuantity ?? 0m;
            var currentInDocument = Items.FirstOrDefault(i => i.ProductId == SelectedProductToAdd.Id);
            decimal alreadyAddedQuantity = currentInDocument?.Quantity ?? 0m;
            decimal totalRequestedQuantity = alreadyAddedQuantity + QuantityToAdd;

            if (totalRequestedQuantity > availableStock)
            {
                string unitName = SelectedProductToAdd.Unit?.Code ?? "од.";
                await _dialogService.ShowErrorAsync(
                    "Недостатньо товару на складі",
                    $"Неможливо списати/продати вказану кількість.\n\n" +
                    $"Товар: «{SelectedProductToAdd.Name}»\n" +
                    $"На складі доступно: {availableStock:0.###} {unitName}\n" +
                    $"Вже в накладній: {alreadyAddedQuantity:0.###} {unitName}\n" +
                    $"Спроба додати: {QuantityToAdd:0.###} {unitName}\n\n" +
                    $"Максимум ще можна додати: {Math.Max(0m, availableStock - alreadyAddedQuantity):0.###} {unitName}.");
                return;
            }
        }

        var existingItem = Items.FirstOrDefault(i => i.ProductId == SelectedProductToAdd.Id);

        if (existingItem != null)
        {
            int index = Items.IndexOf(existingItem);
            var updatedItem = new StockDocumentItem
            {
                Id = existingItem.Id,
                DocumentId = existingItem.DocumentId,
                ProductId = existingItem.ProductId,
                Product = existingItem.Product,
                Quantity = existingItem.Quantity + QuantityToAdd,
                Price = PriceToAdd
            };
            Items[index] = updatedItem;
        }
        else
        {
            var item = new StockDocumentItem
            {
                ProductId = SelectedProductToAdd.Id,
                Product = SelectedProductToAdd,
                Quantity = QuantityToAdd,
                Price = PriceToAdd
            };

            Items.Add(item);
        }

        RecalculateTotal();
        QuantityToAdd = 1;
        UpdateStockLimits();
    }

    [RelayCommand]
    public void IncreaseQuantity()
    {
        decimal step = SelectedProductToAdd?.Unit?.IsFractional == true ? 0.5m : 1m;
        decimal newQuantity = Math.Round(QuantityToAdd + step, 3);

        if (SelectedType is DocumentType.Expense or DocumentType.WriteOff && SelectedProductToAdd != null)
        {
            decimal availableStock = SelectedProductToAdd.StockBalance?.CurrentQuantity ?? 0m;
            var existing = Items.FirstOrDefault(i => i.ProductId == SelectedProductToAdd.Id);
            decimal alreadyInDoc = existing?.Quantity ?? 0m;

            if (alreadyInDoc + newQuantity > availableStock)
            {
                return;
            }
        }

        QuantityToAdd = newQuantity;
    }

    [RelayCommand]
    public void DecreaseQuantity()
    {
        decimal step = SelectedProductToAdd?.Unit?.IsFractional == true ? 0.5m : 1m;
        if (QuantityToAdd - step >= step)
        {
            QuantityToAdd = Math.Round(QuantityToAdd - step, 3);
        }
        else
        {
            QuantityToAdd = step;
        }
    }

    [RelayCommand]
    public void RemoveItem(StockDocumentItem? item)
    {
        if (item != null)
        {
            Items.Remove(item);
            RecalculateTotal();
            UpdateStockLimits();
        }
    }

    private void RecalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.Quantity * i.Price);
    }

    [RelayCommand]
    public async Task ProcessDocumentAsync(Window? window)
    {
        if (Items.Count == 0)
        {
            await _dialogService.ShowInfoAsync("Помилка проведення", "Неможливо провести порожній документ без жодного товару.");
            return;
        }

        try
        {
            var document = new StockDocument
            {
                DocumentNumber = DocumentNumber,
                Date = DateTime.Now,
                Type = SelectedType,
                Counterparty = Counterparty?.Trim() ?? string.Empty,
                Note = Note?.Trim() ?? string.Empty,
                TotalAmount = Items.Sum(i => i.Quantity * i.Price),
                Items = Items.Select(i => new StockDocumentItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            await _documentService.ProcessDocumentAsync(document);
            WeakReferenceMessenger.Default.Send(new DocumentsChangedMessage());

            if (window != null)
            {
                window.DialogResult = true;
                window.Close();
            }
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка проведення", ex.Message);
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