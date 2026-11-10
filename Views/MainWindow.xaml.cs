using WarehouseManagementApp.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel, IContentDialogService dialogService)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (s, e) =>
        {
            if (RootContentDialogHost != null)
            {
                dialogService.SetDialogHost(RootContentDialogHost);
            }

            await viewModel.InitializeAsync();
        };
    }
}