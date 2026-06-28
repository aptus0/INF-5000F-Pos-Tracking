using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SamerHub.Desktop.Services;

public static class PosIntegrationHelpers
{
    public static string? DetectLocalIpv4Address()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(x =>
                x.OperationalStatus == OperationalStatus.Up &&
                x.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(x => x.GetIPProperties().UnicastAddresses)
            .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
            .Select(x => x.Address.ToString())
            .Where(x => !x.StartsWith("169.254.", StringComparison.Ordinal))
            .ToList();

        return candidates.FirstOrDefault();
    }

    public static async Task<string?> ScanSubnetForOpenPortAsync(string ipAddress, int port, CancellationToken cancellationToken)
    {
        var parts = ipAddress.Split('.');
        if (parts.Length != 4)
        {
            throw new InvalidOperationException("PC IP adresi gecersiz.");
        }

        var prefix = $"{parts[0]}.{parts[1]}.{parts[2]}.";

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = Enumerable.Range(1, 254).Select(async host =>
        {
            var candidate = prefix + host;
            using var client = new TcpClient();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(300));
                await client.ConnectAsync(candidate, port, timeoutCts.Token).AsTask();
                linkedCts.Cancel();
                return candidate;
            }
            catch
            {
                return null;
            }
        }).ToList();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            var result = await completed;
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
        }

        return null;
    }
}
