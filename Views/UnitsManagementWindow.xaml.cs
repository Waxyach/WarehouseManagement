using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class UnitsManagementWindow : FluentWindow
{
    public UnitsManagementWindow(UnitsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }
}