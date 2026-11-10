using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;

    public ProductsViewModel ProductsVm { get; }
    public DocumentsViewModel DocumentsVm { get; }

    public MainViewModel(
        ProductsViewModel productsVm,
        DocumentsViewModel documentsVm,
        IWindowService windowService,
        IDialogService dialogService)
    {
        ProductsVm = productsVm;
        DocumentsVm = documentsVm;
        _windowService = windowService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            await Task.WhenAll(
                ProductsVm.LoadDataAsync(),
                DocumentsVm.LoadDocumentsAsync());
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка", $"Помилка завантаження даних: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task OpenCreateDocumentAsync()
    {
        if (_windowService.ShowCreateDocumentWindow() == true)
        {
            await Task.WhenAll(
                ProductsVm.LoadDataAsync(),
                DocumentsVm.LoadDocumentsAsync());
        }
    }
}