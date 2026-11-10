using System.Windows;
using Wpf.Ui.Controls;

namespace WarehouseManagementApp.Services;

public class DialogService : IDialogService
{
    private ContentDialogHost? GetCurrentDialogHost()
    {
        var activeWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
                           ?? Application.Current.MainWindow;

        if (activeWindow == null) return null;

        return activeWindow.FindName("RootContentDialogHost") as ContentDialogHost;
    }

    public async Task ShowInfoAsync(string title, string message)
    {
        var host = GetCurrentDialogHost();
        if (host == null)
        {
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new ContentDialog(host)
        {
            Title = title,
            Content = message,
            CloseButtonText = "Зрозуміло"
        };
        await dialog.ShowAsync();
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var host = GetCurrentDialogHost();
        if (host == null)
        {
            System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var dialog = new ContentDialog(host)
        {
            Title = title,
            Content = message,
            CloseButtonText = "Закрити"
        };
        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Так", string cancelText = "Ні")
    {
        var host = GetCurrentDialogHost();
        if (host == null)
        {
            var res = System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == System.Windows.MessageBoxResult.Yes;
        }

        var dialog = new ContentDialog(host)
        {
            Title = title,
            Content = message,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}