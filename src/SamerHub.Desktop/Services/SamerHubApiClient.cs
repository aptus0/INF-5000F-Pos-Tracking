using System.Net.Http.Json;
using System.Text.Json;
using SamerHub.Core.Contracts;
using SamerHub.Core.Entities;
using SamerHub.Fiscal.Models;
using SamerHub.Pos;

namespace SamerHub.Desktop.Services;

public sealed class SamerHubApiClient
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:7070")
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<PosSettings> GetPosSettingsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/settings/pos", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PosSettings>(JsonOptions, cancellationToken)
            ?? new PosSettings();
    }

    public async Task<PosSettings> SavePosSettingsAsync(PosSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/pos", settings, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PosSettings>(JsonOptions, cancellationToken)
            ?? settings;
    }

    public async Task<PosStatusResult> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/api/pos/test-connection", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PosStatusResult>(JsonOptions, cancellationToken)
            ?? new PosStatusResult();
    }

    public async Task<PosSaleResult> TestSaleAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/api/pos/test-sale", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PosSaleResult>(JsonOptions, cancellationToken)
            ?? new PosSaleResult();
    }

    public async Task<PrinterSettings> GetPrinterSettingsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/settings/printers", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PrinterSettings>(JsonOptions, cancellationToken)
            ?? new PrinterSettings();
    }

    public async Task<PrinterSettings> SavePrinterSettingsAsync(PrinterSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/printers", settings, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PrinterSettings>(JsonOptions, cancellationToken)
            ?? settings;
    }

    public async Task<PrinterTestResult> TestPrinterAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/api/printers/test", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PrinterTestResult>(JsonOptions, cancellationToken)
            ?? new PrinterTestResult();
    }

    public async Task<PlatformApiSettings> GetPlatformSettingsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/settings/platforms", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PlatformApiSettings>(JsonOptions, cancellationToken)
            ?? new PlatformApiSettings();
    }

    public async Task<PlatformApiSettings> SavePlatformSettingsAsync(PlatformApiSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/platforms", settings, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PlatformApiSettings>(JsonOptions, cancellationToken)
            ?? settings;
    }

    public async Task<PlatformConnectionTestResult> TestPlatformAsync(string platformName, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync($"/api/platforms/{platformName}/test", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<PlatformConnectionTestResult>(JsonOptions, cancellationToken)
            ?? new PlatformConnectionTestResult { PlatformName = platformName };
    }

    // Product endpoints
    public async Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/products", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<Product>>(JsonOptions, cancellationToken)
            ?? [];
    }

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

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<Product>(JsonOptions, cancellationToken)
            ?? product;
    }

    public async Task DeleteProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var response = await HttpClient.DeleteAsync($"/api/products/{productId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    // Fiscal settings endpoints
    public async Task<FiscalSettings> GetFiscalSettingsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/settings/fiscal", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<FiscalSettings>(JsonOptions, cancellationToken)
            ?? new FiscalSettings();
    }

    public async Task<FiscalSettings> SaveFiscalSettingsAsync(FiscalSettings settings, CancellationToken cancellationToken)
    {
        var response = await HttpClient.PutAsJsonAsync("/api/settings/fiscal", settings, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<FiscalSettings>(JsonOptions, cancellationToken)
            ?? settings;
    }

    // Fiscal receipt endpoints
    public async Task<List<FiscalReceipt>> GetFiscalReceiptsAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.GetAsync("/api/fiscal/receipts", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<List<FiscalReceipt>>(JsonOptions, cancellationToken)
            ?? [];
    }

    public async Task<FiscalReceiptResult> TestFiscalAsync(CancellationToken cancellationToken)
    {
        var response = await HttpClient.PostAsync("/api/fiscal/test", content: null, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<FiscalReceiptResult>(JsonOptions, cancellationToken)
            ?? new FiscalReceiptResult();
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new HttpRequestException($"API call failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
    }
}
