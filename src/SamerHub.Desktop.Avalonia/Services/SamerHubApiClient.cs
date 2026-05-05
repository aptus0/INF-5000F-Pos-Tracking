using System.Net.Http.Json;
using System.Text.Json;
using SamerHub.Core.Contracts;
using SamerHub.Core.Entities;
using SamerHub.Fiscal.Models;
using SamerHub.Pos;

namespace SamerHub.Desktop.Avalonia.Services;

public sealed class SamerHubApiClient
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://127.0.0.1:7070") };
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<SystemStatusResponse?> GetSystemStatusAsync(CancellationToken cancellationToken) =>
        HttpClient.GetFromJsonAsync<SystemStatusResponse>("/api/system/status", JsonOptions, cancellationToken);

    public Task<DashboardSummaryResponse?> GetDashboardSummaryAsync(CancellationToken cancellationToken) =>
        HttpClient.GetFromJsonAsync<DashboardSummaryResponse>("/api/dashboard/summary", JsonOptions, cancellationToken);

    public async Task<List<DiningTable>> GetTablesAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<DiningTable>>("/api/tables", JsonOptions, cancellationToken) ?? [];

    public async Task<DiningTable> CreateTableAsync(CreateTableRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/tables", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiningTable>(JsonOptions, cancellationToken) ?? new DiningTable();
    }

    public async Task<DiningTable> UpdateTableAsync(Guid id, UpdateTableRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync($"/api/tables/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiningTable>(JsonOptions, cancellationToken) ?? new DiningTable();
    }

    public async Task<TableSession> OpenTableSessionAsync(Guid tableId, string tableName, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync($"/api/tables/{tableId}/open-session", new OpenTableSessionRequest { TableName = tableName }, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<TableSession?> CloseTableSessionAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/api/tables/{tableId}/close-session", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken);
    }

    public async Task<TableSession?> GetTableSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<TableSession>($"/api/table-sessions/{sessionId}", JsonOptions, cancellationToken);

    public async Task<TableSession> AddTableItemAsync(Guid sessionId, TableSessionItemRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync($"/api/table-sessions/{sessionId}/items", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<TableSession> UpdateTableItemAsync(Guid itemId, UpdateTableOrderItemRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync($"/api/table-order-items/{itemId}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<TableSession> DeleteTableItemAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.DeleteAsync($"/api/table-order-items/{itemId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<TableSession> SendKitchenAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/api/table-sessions/{sessionId}/send-kitchen", null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<TableSession> RequestPaymentAsync(Guid sessionId, PaymentRequest request, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync($"/api/table-sessions/{sessionId}/payment", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TableSession>(JsonOptions, cancellationToken) ?? new TableSession();
    }

    public async Task<List<TableSession>> GetCashierOpenPaymentsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<TableSession>>("/api/cashier/open-payments", JsonOptions, cancellationToken) ?? [];

    public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<Product>>("/api/products", JsonOptions, cancellationToken) ?? [];

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<Category>>("/api/categories", JsonOptions, cancellationToken) ?? [];

    public async Task<Product> SaveProductAsync(Product product, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        if (product.Id == Guid.Empty)
        {
            response = await HttpClient.PostAsJsonAsync("/api/products", product, JsonOptions, cancellationToken);
        }
        else
        {
            response = await HttpClient.PutAsJsonAsync($"/api/products/{product.Id}", product, JsonOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Product>(JsonOptions, cancellationToken) ?? product;
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.DeleteAsync($"/api/products/{productId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Order>> GetOrdersAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<Order>>("/api/orders", JsonOptions, cancellationToken) ?? [];

    public async Task<PosSettings> GetPosSettingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<PosSettings>("/api/pos/settings", JsonOptions, cancellationToken) ?? new PosSettings();

    public async Task<PosSettings> SavePosSettingsAsync(PosSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/pos/settings", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PosSettings>(JsonOptions, cancellationToken) ?? settings;
    }

    public async Task<PrinterSettings> GetPrinterSettingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<PrinterSettings>("/api/settings/printers", JsonOptions, cancellationToken) ?? new PrinterSettings();

    public async Task<PrinterSettings> SavePrinterSettingsAsync(PrinterSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/printers", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PrinterSettings>(JsonOptions, cancellationToken) ?? settings;
    }

    public async Task<PlatformApiSettings> GetPlatformSettingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<PlatformApiSettings>("/api/settings/platforms", JsonOptions, cancellationToken) ?? new PlatformApiSettings();

    public async Task<PlatformApiSettings> SavePlatformSettingsAsync(PlatformApiSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/platforms", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlatformApiSettings>(JsonOptions, cancellationToken) ?? settings;
    }

    public async Task<FiscalSettings> GetFiscalSettingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<FiscalSettings>("/api/fiscal/settings", JsonOptions, cancellationToken) ?? new FiscalSettings();

    public async Task<FiscalSettings> SaveFiscalSettingsAsync(FiscalSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/fiscal/settings", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FiscalSettings>(JsonOptions, cancellationToken) ?? settings;
    }

    public async Task<PosStatusResult> TestPosConnectionAsync(CancellationToken cancellationToken) =>
        await PostAsync<PosStatusResult>("/api/pos/test-connection", cancellationToken) ?? new PosStatusResult();

    public async Task<PosSaleResult> TestPosSaleAsync(CancellationToken cancellationToken) =>
        await PostAsync<PosSaleResult>("/api/pos/test-sale", cancellationToken) ?? new PosSaleResult();

    public async Task<FiscalReceiptResult> TestFiscalReceiptAsync(CancellationToken cancellationToken) =>
        await PostAsync<FiscalReceiptResult>("/api/fiscal/test-receipt", cancellationToken) ?? new FiscalReceiptResult();

    public async Task<List<VatDepartmentMapping>> GetVatMappingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<VatDepartmentMapping>>("/api/fiscal/vat-mappings", JsonOptions, cancellationToken) ?? [];

    public async Task SaveVatMappingsAsync(List<VatDepartmentMapping> mappings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/fiscal/vat-mappings", mappings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<PaymentTypeMapping>> GetPaymentMappingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<List<PaymentTypeMapping>>("/api/fiscal/payment-mappings", JsonOptions, cancellationToken) ?? [];

    public async Task SavePaymentMappingsAsync(List<PaymentTypeMapping> mappings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/fiscal/payment-mappings", mappings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DatabaseConnectionSettings> GetDatabaseSettingsAsync(CancellationToken cancellationToken) =>
        await HttpClient.GetFromJsonAsync<DatabaseConnectionSettings>("/api/database/settings", JsonOptions, cancellationToken) ?? new DatabaseConnectionSettings();

    public async Task<DatabaseConnectionSettings> SaveDatabaseSettingsAsync(DatabaseConnectionSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/database/settings", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DatabaseConnectionSettings>(JsonOptions, cancellationToken) ?? settings;
    }

    public async Task<DatabaseTestResult> TestDatabaseAsync(DatabaseConnectionSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync("/api/database/test", settings, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DatabaseTestResult>(JsonOptions, cancellationToken) ?? new DatabaseTestResult();
    }

    public async Task MigrateDatabaseAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/api/database/migrate", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<TableSession> PayCashAsync(PaymentRequest request, CancellationToken cancellationToken) =>
        await PostJsonAsync<TableSession>("/api/payments/cash", request, cancellationToken) ?? new TableSession();

    public async Task<TableSession> PayCardAsync(PaymentRequest request, CancellationToken cancellationToken) =>
        await PostJsonAsync<TableSession>("/api/payments/card", request, cancellationToken) ?? new TableSession();

    public async Task<TableSession> PaySplitAsync(SplitPaymentRequest request, CancellationToken cancellationToken) =>
        await PostJsonAsync<TableSession>("/api/payments/split", request, cancellationToken) ?? new TableSession();

    public async Task<string> DetectLocalIpAsync(CancellationToken cancellationToken)
    {
        var response = await PostAsync<LocalIpResponse>("/api/pos/detect-local-ip", cancellationToken);
        return response?.LocalIp ?? "127.0.0.1";
    }

    private static async Task<T?> PostAsync<T>(string route, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync(route, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task<T?> PostJsonAsync<T>(string route, object payload, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsJsonAsync(route, payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private sealed class LocalIpResponse
    {
        public string LocalIp { get; set; } = string.Empty;
    }
}
