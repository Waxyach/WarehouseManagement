namespace WarehouseManagementApp.Services;

public interface IDialogService
{
    Task ShowInfoAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Так", string cancelText = "Ні");
}