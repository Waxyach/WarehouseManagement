using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class CategoryViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly Category? _originalCategory;

    [ObservableProperty]
    private string _title = "Створення категорії";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _colorHex = "#3B82F6";

    [ObservableProperty]
    private ObservableCollection<string> _recentColors = new();

    [ObservableProperty]
    private bool _hasRecentColors;

    public Category ResultCategory { get; private set; } = null!;

    public CategoryViewModel(IDialogService dialogService, Category? existingCategory = null)
    {
        _dialogService = dialogService;
        _originalCategory = existingCategory;

        if (existingCategory != null)
        {
            Title = "Редагування категорії";
            Name = existingCategory.Name;
            ColorHex = string.IsNullOrWhiteSpace(existingCategory.ColorHex) ? "#EF4444" : existingCategory.ColorHex;
        }
        else
        {
            Title = "Створення категорії";
            ColorHex = "#3B82F6";
        }

        RefreshRecentColors();
    }

    public void RefreshRecentColors()
    {
        RecentColors = new ObservableCollection<string>(ColorPaletteHelper.CustomColors);
        HasRecentColors = RecentColors.Count > 0;
    }

    [RelayCommand]
    private void SelectPresetColor(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            ColorHex = hex;
        }
    }

    [RelayCommand]
    private async Task SaveAsync(Window? window)
    {
        string trimmedName = Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            await _dialogService.ShowInfoAsync("Увага", "Введіть назву категорії.");
            return;
        }

        ColorPaletteHelper.AddCustomColor(ColorHex);

        ResultCategory = _originalCategory ?? new Category();
        ResultCategory.Name = trimmedName;
        ResultCategory.ColorHex = ColorHex;

        if (window != null)
        {
            window.DialogResult = true;
            window.Close();
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