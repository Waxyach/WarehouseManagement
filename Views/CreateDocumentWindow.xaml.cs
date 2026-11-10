using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class CreateDocumentWindow : FluentWindow
{
    public CreateDocumentWindow(CreateDocumentViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) =>
        {
            await viewModel.InitializeAsync();
        };
    }
}