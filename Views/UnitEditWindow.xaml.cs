using WarehouseManagementApp.Models;
using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class UnitEditWindow : FluentWindow
{
    private readonly UnitEditViewModel _viewModel;

    public Unit ResultUnit => _viewModel.ResultUnit;

    public UnitEditWindow(UnitEditViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }
}