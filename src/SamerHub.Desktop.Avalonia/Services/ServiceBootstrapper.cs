using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Http.Json;
using SamerHub.Core.Contracts;

namespace SamerHub.Desktop.Avalonia.Services;

public sealed class ServiceBootstrapper(AppSettingsService settingsService)
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://127.0.0.1:7070") };

    public async Task<ServiceStartupResult> EnsureServiceAvailableAsync()
    {
        var initialHealth = await TryGetHealthAsync();
        if (initialHealth is not null)
        {
            return new ServiceStartupResult(true, "Servis zaten calisiyor.", initialHealth);
        }

        if (OperatingSystem.IsWindows())
        {
            TryStartWindowsService();
            await Task.Delay(1500);
            var secondHealth = await TryGetHealthAsync();
            if (secondHealth is not null)
            {
                return new ServiceStartupResult(true, "Servis masaustu uygulamasi tarafindan dogrulandi.", secondHealth);
            }
        }

        var settings = settingsService.Load();
        if (await IsPortOccupiedAsync())
        {
            return new ServiceStartupResult(false, "127.0.0.1:7070 portu cevap veriyor ancak SAMER Hub Service health yaniti alinmadi. Port baska bir surec tarafindan kullaniliyor olabilir.", null);
        }

        return new ServiceStartupResult(false, $"Servise baglanilamadi. Beklenen URL: http://127.0.0.1:7070. Secili DB provider: {settings.Database.Provider}.", null);
    }

    private static async Task<ServiceHealthResponse?> TryGetHealthAsync()
    {
        try
        {
            return await HttpClient.GetFromJsonAsync<ServiceHealthResponse>("/api/health");
        }
        catch
        {
            return null;
        }
    }

    private static void TryStartWindowsService()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = "start SamerHubService",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
            // Keep desktop usable in offline/developer scenarios.
        }
    }

    private static async Task<bool> IsPortOccupiedAsync()
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync("127.0.0.1", 7070);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}

public sealed record ServiceStartupResult(bool IsConnected, string Message, ServiceHealthResponse? Health);
