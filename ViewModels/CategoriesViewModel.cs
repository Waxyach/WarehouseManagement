using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IDialogService _dialogService;
    private readonly IWindowService _windowService;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditCategoryCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCategoryCommand))]
    private Category? _selectedCategory;

    public CategoriesViewModel(
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
        var items = await _productService.GetAllCategoriesAsync();
        Categories = new ObservableCollection<Category>(items);
        SelectedCategory = null;
    }

    [RelayCommand]
    private async Task AddCategoryAsync(Window? ownerWindow = null)
    {
        var created = _windowService.ShowCategoryEditorWindow();
        if (created != null)
        {
            try
            {
                await _productService.CreateCategoryAsync(created);
                await LoadDataAsync();
                WeakReferenceMessenger.Default.Send(new DictionariesChangedMessage());
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorAsync("Помилка створення", ex.Message);
            }
        }
    }

    private bool HasSelection() => SelectedCategory != null;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditCategoryAsync(Window? ownerWindow = null)
    {
        if (SelectedCategory == null) return;

        var edited = _windowService.ShowCategoryEditorWindow(SelectedCategory);
        if (edited != null)
        {
            try
            {
                await _productService.UpdateCategoryAsync(edited);
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
    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory == null) return;

        bool confirmed = await _dialogService.ShowConfirmationAsync(
            "Підтвердження видалення",
            $"Ви дійсно бажаєте видалити категорію «{SelectedCategory.Name}»?",
            "Видалити",
            "Скасувати");

        if (!confirmed) return;

        var (success, error) = await _productService.DeleteCategoryAsync(SelectedCategory.Id);
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