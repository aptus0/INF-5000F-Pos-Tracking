namespace SamerHub.Core.Entities;

public sealed class PlatformApiSettings
{
    public PlatformConnectionSettings TrendyolGo { get; set; } = new();
    public PlatformConnectionSettings Getir { get; set; } = new();
    public PlatformConnectionSettings Yemeksepeti { get; set; } = new();
}

public sealed class PlatformConnectionSettings
{
    public bool IsEnabled { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string LastTestResult { get; set; } = string.Empty;
    public DateTimeOffset? LastTestAtUtc { get; set; }
}
