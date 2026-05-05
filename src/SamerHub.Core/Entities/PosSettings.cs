namespace SamerHub.Core.Entities;

public sealed class PosSettings
{
    public bool IsEnabled { get; set; }
    public string Mode { get; set; } = "Fake";
    public string PcIpAddress { get; set; } = string.Empty;
    public string PosIpAddress { get; set; } = "192.168.50.20";
    public int PosPort { get; set; } = 5000;
    public int TimeoutSeconds { get; set; } = 60;
    public string CurrencyCode { get; set; } = "TRY";
    public int CurrencyNumericCode { get; set; } = 949;
    public string DefaultPaymentMethod { get; set; } = "CardContactlessAndChip";
    public decimal MinimumTestAmount { get; set; } = 1.00m;
    public string TerminalId { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string EcrSecret { get; set; } = string.Empty;
    public string Protocol { get; set; } = "RawTcp";
    public string ConnectionType { get; set; } = "DirectCable";
    public bool ChecksumEnabled { get; set; }
    public bool RawTcpLoggingEnabled { get; set; }
    public int RetryCount { get; set; } = 1;
    public bool LastConnectionSucceeded { get; set; }
    public DateTimeOffset? LastConnectionAtUtc { get; set; }
    public long? LastRoundTripMilliseconds { get; set; }
    public string LastError { get; set; } = string.Empty;
    public string LastRawResponse { get; set; } = string.Empty;
}
