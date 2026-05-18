namespace SamerHub.Core.Contracts;

public sealed class PosBridgeStatusUpdateRequest
{
    public string TerminalId { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public string LanIpAddress { get; set; } = string.Empty;
    public bool IsConnectedToHub { get; set; }
    public bool IsApiSubscribed { get; set; }
    public bool IsTransactionServiceConnected { get; set; }
    public bool IsReady { get; set; }
    public DateTimeOffset? TokenExpiresAtUtc { get; set; }
    public string LastError { get; set; } = string.Empty;
    public List<string> AvailableCurrencies { get; set; } = [];
    public List<string> AvailableTransactionTypes { get; set; } = [];
    public List<string> AvailablePaymentMeans { get; set; } = [];
}
