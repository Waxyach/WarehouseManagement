using WarehouseManagementApp.ViewModels;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Views;

public partial class DocumentDetailsWindow : FluentWindow
{
    public DocumentDetailsWindow(DocumentDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}