using System.Collections.ObjectModel;
using SamerHub.Core.Contracts;
using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Desktop.Avalonia.Services;

namespace SamerHub.Desktop.Avalonia.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly SamerHubApiClient _apiClient;
    private readonly AppSettingsService _appSettingsService;
    private string _serviceMessage;
    private string _serviceStatus;
    private string _footerStatus;
    private bool _isBusy;
    private PosSettings _posSettings = new();
    private FiscalSettings _fiscalSettings = new();
    private PrinterSettings _printerSettings = new();
    private PlatformApiSettings _platformSettings = new();
    private DatabaseConnectionSettings _databaseSettings = new();
    private DashboardSummaryResponse _dashboard = new();
    private DiningTable? _selectedTable;
    private TableSession? _selectedSession;
    private Product? _selectedProduct;
    private Product _draftProduct = new();
    private CreateTableRequest _draftTable = new();
    private string _cashAmount = "0";
    private string _cardAmount = "0";
    private string _posFooter = "Bekleniyor";
    private string _internetFooter = "Kontrol";
    private string _printerFooter = "Bekleniyor";
    private string _userFooter = "Yonetici";
    private string _branchFooter = "Merkez";
    private string _clockFooter = DateTime.Now.ToString("dd.MM.yyyy HH:mm");

    public MainWindowViewModel(SamerHubApiClient apiClient, AppSettingsService appSettingsService, ServiceStartupResult startup)
    {
        _apiClient = apiClient;
        _appSettingsService = appSettingsService;
        _serviceMessage = startup.Message;
        _serviceStatus = startup.IsConnected ? "Servis Bagli" : "Servis Bekleniyor";
        _footerStatus = startup.Health is null ? "Local service offline" : $"Version {startup.Health.Version}";
        _ = LoadAsync();
    }

    public ObservableCollection<DiningTable> Tables { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];
    public ObservableCollection<Order> Orders { get; } = [];
    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<TableSession> CashierSessions { get; } = [];
    public ObservableCollection<VatDepartmentMapping> VatMappings { get; } = [];
    public ObservableCollection<PaymentTypeMapping> PaymentMappings { get; } = [];

    public string ServiceMessage { get => _serviceMessage; set => SetProperty(ref _serviceMessage, value); }
    public string ServiceStatus { get => _serviceStatus; set => SetProperty(ref _serviceStatus, value); }
    public string FooterStatus { get => _footerStatus; set => SetProperty(ref _footerStatus, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public PosSettings PosSettings { get => _posSettings; set => SetProperty(ref _posSettings, value); }
    public FiscalSettings FiscalSettings { get => _fiscalSettings; set => SetProperty(ref _fiscalSettings, value); }
    public PrinterSettings PrinterSettings { get => _printerSettings; set => SetProperty(ref _printerSettings, value); }
    public PlatformApiSettings PlatformSettings { get => _platformSettings; set => SetProperty(ref _platformSettings, value); }
    public DatabaseConnectionSettings DatabaseSettings { get => _databaseSettings; set => SetProperty(ref _databaseSettings, value); }
    public DashboardSummaryResponse Dashboard { get => _dashboard; set => SetProperty(ref _dashboard, value); }
    public DiningTable? SelectedTable { get => _selectedTable; set => SetProperty(ref _selectedTable, value); }
    public TableSession? SelectedSession { get => _selectedSession; set => SetProperty(ref _selectedSession, value); }
    public Product? SelectedProduct { get => _selectedProduct; set => SetProperty(ref _selectedProduct, value); }
    public Product DraftProduct { get => _draftProduct; set => SetProperty(ref _draftProduct, value); }
    public CreateTableRequest DraftTable { get => _draftTable; set => SetProperty(ref _draftTable, value); }
    public string CashAmount { get => _cashAmount; set => SetProperty(ref _cashAmount, value); }
    public string CardAmount { get => _cardAmount; set => SetProperty(ref _cardAmount, value); }
    public string PosFooter { get => _posFooter; set => SetProperty(ref _posFooter, value); }
    public string InternetFooter { get => _internetFooter; set => SetProperty(ref _internetFooter, value); }
    public string PrinterFooter { get => _printerFooter; set => SetProperty(ref _printerFooter, value); }
    public string UserFooter { get => _userFooter; set => SetProperty(ref _userFooter, value); }
    public string BranchFooter { get => _branchFooter; set => SetProperty(ref _branchFooter, value); }
    public string ClockFooter { get => _clockFooter; set => SetProperty(ref _clockFooter, value); }

    public async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            ClockFooter = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var status = await _apiClient.GetSystemStatusAsync(cts.Token);
            Dashboard = await _apiClient.GetDashboardSummaryAsync(cts.Token) ?? new DashboardSummaryResponse();
            var tables = await _apiClient.GetTablesAsync(cts.Token);
            var products = await _apiClient.GetProductsAsync(cts.Token);
            var orders = await _apiClient.GetOrdersAsync(cts.Token);
            var categories = await _apiClient.GetCategoriesAsync(cts.Token);
            var cashier = await _apiClient.GetCashierOpenPaymentsAsync(cts.Token);

            PosSettings = await _apiClient.GetPosSettingsAsync(cts.Token);
            FiscalSettings = await _apiClient.GetFiscalSettingsAsync(cts.Token);
            PrinterSettings = await _apiClient.GetPrinterSettingsAsync(cts.Token);
            PlatformSettings = await _apiClient.GetPlatformSettingsAsync(cts.Token);
            DatabaseSettings = await _apiClient.GetDatabaseSettingsAsync(cts.Token);

            ResetCollection(Tables, tables);
            ResetCollection(Products, products);
            ResetCollection(Orders, orders);
            ResetCollection(Categories, categories);
            ResetCollection(CashierSessions, cashier);
            ResetCollection(VatMappings, await _apiClient.GetVatMappingsAsync(cts.Token));
            ResetCollection(PaymentMappings, await _apiClient.GetPaymentMappingsAsync(cts.Token));

            if (SelectedTable is not null)
            {
                SelectedTable = Tables.FirstOrDefault(x => x.Id == SelectedTable.Id);
            }

            if (SelectedSession is not null)
            {
                SelectedSession = await _apiClient.GetTableSessionAsync(SelectedSession.Id, cts.Token);
            }

            if (status is not null)
            {
                ServiceStatus = status.PortBinding.IsCurrentInstanceServing ? "Servis Bagli" : "Port Uyarisi";
                FooterStatus = $"DB: {status.Database.Provider} | POS: {status.Pos.Status} | Fiscal: {status.Fiscal.Status}";
                PosFooter = status.Pos.Status;
                InternetFooter = status.Internet.Status;
                PrinterFooter = status.Printer.Status;
            }
        }
        catch (Exception ex)
        {
            ServiceStatus = "Servis Hatasi";
            ServiceMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        PosSettings = await _apiClient.SavePosSettingsAsync(PosSettings, cts.Token);
        FiscalSettings = await _apiClient.SaveFiscalSettingsAsync(FiscalSettings, cts.Token);
        PrinterSettings = await _apiClient.SavePrinterSettingsAsync(PrinterSettings, cts.Token);
        PlatformSettings = await _apiClient.SavePlatformSettingsAsync(PlatformSettings, cts.Token);
        DatabaseSettings = await _apiClient.SaveDatabaseSettingsAsync(DatabaseSettings, cts.Token);
        await _apiClient.SaveVatMappingsAsync(VatMappings.ToList(), cts.Token);
        await _apiClient.SavePaymentMappingsAsync(PaymentMappings.ToList(), cts.Token);
        FooterStatus = "Ayarlar kaydedildi";
    }

    public async Task TestPosAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await _apiClient.TestPosConnectionAsync(cts.Token);
        ServiceMessage = result.IsReachable ? "POS baglantisi basarili." : result.ErrorMessage;
    }

    public async Task TestPosSaleAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await _apiClient.TestPosSaleAsync(cts.Token);
        ServiceMessage = result.IsSuccessful ? "1 TL test satis denemesi tamamlandi." : result.ErrorMessage;
    }

    public async Task TestFiscalAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await _apiClient.TestFiscalReceiptAsync(cts.Token);
        ServiceMessage = result.IsSuccessful ? $"Mali fis denemesi basarili. Fis No: {result.ReceiptNo}" : result.ErrorMessage;
    }

    public async Task TestDatabaseAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var result = await _apiClient.TestDatabaseAsync(DatabaseSettings, cts.Token);
        ServiceMessage = result.Message;
    }

    public async Task CreateTableAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var created = await _apiClient.CreateTableAsync(DraftTable, cts.Token);
        Tables.Add(created);
        DraftTable = new CreateTableRequest();
        ServiceMessage = "Yeni masa eklendi.";
    }

    public async Task OpenSelectedTableAsync()
    {
        if (SelectedTable is null)
        {
            ServiceMessage = "Bir masa sec.";
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        SelectedSession = await _apiClient.OpenTableSessionAsync(SelectedTable.Id, SelectedTable.Name, cts.Token);
        await LoadAsync();
        ServiceMessage = $"{SelectedTable.Name} icin oturum acildi.";
    }

    public async Task AddSelectedProductToSessionAsync()
    {
        if (SelectedSession is null || SelectedProduct is null)
        {
            ServiceMessage = "Once masa oturumu ve urun sec.";
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        SelectedSession = await _apiClient.AddTableItemAsync(SelectedSession.Id, new TableSessionItemRequest
        {
            ProductId = SelectedProduct.Id,
            Quantity = 1
        }, cts.Token);
        await LoadAsync();
        ServiceMessage = $"{SelectedProduct.Name} sepete eklendi.";
    }

    public async Task SendKitchenAsync()
    {
        if (SelectedSession is null)
        {
            ServiceMessage = "Aktif masa oturumu sec.";
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        SelectedSession = await _apiClient.SendKitchenAsync(SelectedSession.Id, cts.Token);
        await LoadAsync();
        ServiceMessage = "Mutfak fis akisi baslatildi.";
    }

    public async Task PayCashAsync()
    {
        if (SelectedSession is null || !decimal.TryParse(CashAmount, out var amount))
        {
            ServiceMessage = "Gecerli nakit tutar gir.";
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        SelectedSession = await _apiClient.PayCashAsync(new PaymentRequest
        {
            TableSessionId = SelectedSession.Id,
            Method = PaymentMethod.Cash,
            Amount = amount
        }, cts.Token);
        await LoadAsync();
        ServiceMessage = "Nakit odeme kaydedildi.";
    }

    public async Task PayCardAsync()
    {
        if (SelectedSession is null || !decimal.TryParse(CardAmount, out var amount))
        {
            ServiceMessage = "Gecerli kart tutar gir.";
            return;
        }

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        SelectedSession = await _apiClient.PayCardAsync(new PaymentRequest
        {
            TableSessionId = SelectedSession.Id,
            Method = PaymentMethod.Card,
            Amount = amount
        }, cts.Token);
        await LoadAsync();
        ServiceMessage = "Kart odeme akisi tamamlandi.";
    }

    public async Task SaveProductAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        DraftProduct = await _apiClient.SaveProductAsync(DraftProduct, cts.Token);
        ServiceMessage = "Urun kaydedildi.";
        DraftProduct = new Product();
        await LoadAsync();
    }

    public async Task AutofillLocalIpAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        PosSettings.PcIpAddress = await _apiClient.DetectLocalIpAsync(cts.Token);
        RaisePropertyChanged(nameof(PosSettings));
    }

    public void OpenWizard()
    {
        _appSettingsService.SetSkipWizardOnStartup(false);
        ServiceMessage = "Wizard bir sonraki acilista tekrar gorunecek.";
    }

    private static void ResetCollection<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
