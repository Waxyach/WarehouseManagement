using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagementApp.Data;
using WarehouseManagementApp.Services;
using WarehouseManagementApp.ViewModels;
using WarehouseManagementApp.Views;
using Wpf.Ui;

namespace WarehouseManagementApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        string connectionString = WarehouseDbContext.BuildDefaultConnectionString();

        services.AddDbContextFactory<WarehouseDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddSingleton<IContentDialogService, ContentDialogService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();

        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IDocumentService, DocumentService>();
        services.AddSingleton<IReportService, ReportService>();

        services.AddTransient<CategoriesViewModel>();
        services.AddTransient<CategoriesManagementWindow>();
        services.AddTransient<UnitsViewModel>();
        services.AddTransient<UnitsManagementWindow>();

        services.AddTransient<CreateProductViewModel>();
        services.AddTransient<CreateDocumentViewModel>();
        services.AddTransient<ProductsViewModel>();
        services.AddTransient<DocumentsViewModel>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var contextFactory = Services.GetRequiredService<IDbContextFactory<WarehouseDbContext>>();
            DatabaseInitializer.Initialize(contextFactory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Помилка підключення до бази даних SQLite:\n{ex.Message}",
                "Помилка БД",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
}