using WarehouseManagementApp.Models;
using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class EditProductWindow : FluentWindow
{
    private readonly EditProductViewModel _viewModel;

    public Product ResultProduct => _viewModel.ResultProduct;

    public EditProductWindow(EditProductViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}