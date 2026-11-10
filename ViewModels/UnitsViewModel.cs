using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class UnitsViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private ObservableCollection<Unit> _units = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditUnitCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteUnitCommand))]
    private Unit? _selectedUnit;

    public UnitsViewModel(
        IProductService productService,
        IDialogService dialogService,
        IWindowService windowService)
    {
        _productService = productService;
        _dialogService = dialogService;
        _windowService = windowService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        var items = await _productService.GetAllUnitsAsync();
        Units = new ObservableCollection<Unit>(items);
        SelectedUnit = null;
    }

    [RelayCommand]
    private async Task AddUnitAsync(Window? ownerWindow = null)
    {
        var created = _windowService.ShowUnitEditorWindow();
        if (created != null)
        {
            try
            {
                await _productService.CreateUnitAsync(created);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DictionariesChangedMessage());
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка створення", ex.Message);
            }
        }
    }

    private bool HasSelection() => SelectedUnit != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditUnitAsync(Window? ownerWindow = null)
    {
        if (SelectedUnit == null) return;

        var edited = _windowService.ShowUnitEditorWindow(SelectedUnit);
        if (edited != null)
        {
            try
            {
                await _productService.UpdateUnitAsync(edited);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DictionariesChangedMessage());
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка редагування", ex.Message);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteUnitAsync()
    {
        if (SelectedUnit == null) return;

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Підтвердження видалення",
            $"Ви дійсно бажаєте видалити одиницю «{SelectedUnit.Name}»?",
            "Видалити",
            "Скасувати");

        if (!confirmed) return;

        var (success, error) = await _productService.DeleteUnitAsync(SelectedUnit.Id);
        if (!success)
        {
            await _dialogService.ShowErrorAsync("Неможливо видалити", error);
            return;
        }

        await LoadDataAsync();
        WeakReferenceMessenger.Default.Send(new DictionariesChangedMessage());
    }

    [RelayCommand]
    private void Close(Window? window)
    {
        if (window != null)
        {
            window.DialogResult = true;
            window.Close();
        }
    }
}