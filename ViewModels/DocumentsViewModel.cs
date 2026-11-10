using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using WarehouseManagementApp.Helpers;
using WarehouseManagementApp.Models;
using WarehouseManagementApp.Services;

namespace WarehouseManagementApp.ViewModels;

public partial class DocumentsViewModel : ObservableObject,
    IRecipient<ProductsChangedMessage>,
    IRecipient<DictionariesChangedMessage>,
    IRecipient<DocumentsChangedMessage>
{
    private readonly IDocumentService _documentService;
    private readonly IWindowService _windowService;
    private readonly IDialogService _dialogService;
    private List<StockDocument> _allDocuments = new();

    [ObservableProperty]
    private ObservableCollection<StockDocument> _documents = new();

    [ObservableProperty]
    private StockDocument? _selectedDocument;

    [ObservableProperty]
    private string _searchCounterparty = string.Empty;

    [ObservableProperty]
    private DateTime? _startDate;

    [ObservableProperty]
    private DateTime? _endDate;

    [ObservableProperty]
    private bool _isLoading;

    public DocumentsViewModel(
        IDocumentService documentService,
        IWindowService windowService,
        IDialogService dialogService)
    {
        _documentService = documentService;
        _windowService = windowService;
        _dialogService = dialogService;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(ProductsChangedMessage message) => _ = LoadDocumentsAsync();

    public void Receive(DictionariesChangedMessage message) => _ = LoadDocumentsAsync();

    public void Receive(DocumentsChangedMessage message) => _ = LoadDocumentsAsync();

    [RelayCommand]
    public async Task LoadDocumentsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            int? previousSelectedDocId = SelectedDocument?.Id;
            _allDocuments = await _documentService.GetAllDocumentsAsync();
            ApplyFilter(previousSelectedDocId);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Помилка завантаження", $"Не вдалося завантажити документи: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CreateDocumentAsync()
    {
        if (_windowService.ShowCreateDocumentWindow() == true)
        {
            await LoadDocumentsAsync();
        }
    }

    [RelayCommand]
    private async Task OpenSelectedDetailsAsync()
    {
        if (SelectedDocument != null)
        {
            _windowService.ShowDocumentDetailsWindow(SelectedDocument);
        }
        else
        {
            await _dialogService.ShowInfoAsync("Увага", "Оберіть документ у журналі для перегляду деталей.");
        }
    }

    [RelayCommand]
    private void ShowDocumentDetails(StockDocument? document)
    {
        if (document != null)
        {
            _windowService.ShowDocumentDetailsWindow(document);
        }
    }

    partial void OnSearchCounterpartyChanged(string value) => ApplyFilter();
    partial void OnStartDateChanged(DateTime? value) => ApplyFilter();
    partial void OnEndDateChanged(DateTime? value) => ApplyFilter();

    [RelayCommand]
    public void ResetDateFilter()
    {
        StartDate = null;
        EndDate = null;
    }

    private void ApplyFilter(int? restoreDocumentId = null)
    {
        int? targetId = restoreDocumentId ?? SelectedDocument?.Id;
        IEnumerable<StockDocument> filtered = _allDocuments;

        if (!string.IsNullOrWhiteSpace(SearchCounterparty))
        {
            string query = SearchCounterparty.Trim();
            filtered = filtered.Where(d =>
                (!string.IsNullOrEmpty(d.Counterparty) && d.Counterparty.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(d.DocumentNumber) && d.DocumentNumber.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        if (StartDate.HasValue)
        {
            var from = StartDate.Value.Date;
            filtered = filtered.Where(d => d.Date.Date >= from);
        }

        if (EndDate.HasValue)
        {
            var to = EndDate.Value.Date;
            filtered = filtered.Where(d => d.Date.Date <= to);
        }

        Documents = new ObservableCollection<StockDocument>(filtered.OrderByDescending(d => d.Date));

        if (targetId.HasValue)
        {
            SelectedDocument = Documents.FirstOrDefault(d => d.Id == targetId.Value);
        }
    }
}