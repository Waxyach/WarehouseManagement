using WarehouseManagementApp.Models;
using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class CreateProductWindow : FluentWindow
{
    private readonly CreateProductViewModel _viewModel;

    public Product? ResultProduct => _viewModel.ResultProduct;

    public CreateProductWindow(CreateProductViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}