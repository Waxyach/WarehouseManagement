using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.ViewModels;

public partial class CategoryGroupItemViewModel : ObservableObject
{
    public Category Category { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<Product> Products { get; }

    public CategoryGroupItemViewModel(Category category, IEnumerable<Product> products)
    {
        Category = category;
        Products = new ObservableCollection<Product>(products);
    }
}