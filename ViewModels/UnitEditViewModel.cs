using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class UnitEditViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly Unit? _originalUnit;

    [ObservableProperty]
    private string _title = "Створення одиниці виміру";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private bool _isFractional;

    public Unit ResultUnit { get; private set; } = null!;

    public UnitEditViewModel(IDialogService dialogService, Unit? existingUnit = null)
    {
        _dialogService = dialogService;
        _originalUnit = existingUnit;

        if (existingUnit != null)
        {
            Title = "Редагування одиниці виміру";
            Name = existingUnit.Name;
            Code = existingUnit.Code;
            IsFractional = existingUnit.IsFractional;
        }
        else
        {
            Title = "Створення одиниці виміру";
        }
    }

    [RelayCommand]
    private async Task SaveAsync(Window? window)
    {
        string trimmedName = Name?.Trim() ?? string.Empty;
        string trimmedCode = Code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName) || string.IsNullOrWhiteSpace(trimmedCode))
        {
            await _dialogService.ShowInfoAsync("Увага", "Заповніть назву та символ одиниці.");
            return;
        }

        ResultUnit = _originalUnit ?? new Unit();
        ResultUnit.Name = trimmedName;
        ResultUnit.Code = trimmedCode;
        ResultUnit.IsFractional = IsFractional;

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