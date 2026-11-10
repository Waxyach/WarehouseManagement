using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.ViewModels;

public partial class DocumentDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    private StockDocument _document;

    public DocumentDetailsViewModel(StockDocument document)
    {
        _document = document;
    }

    [RelayCommand]
    private void Close(Window? window)
    {
        if (window != null)
        {
            window.Close();
        }
    }
}