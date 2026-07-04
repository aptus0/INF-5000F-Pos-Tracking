namespace SamerHub.Infrastructure.Persistence;

public sealed class AppSettingRecord
{
    public string Key { get; set; } = string.Empty;
    public string JsonValue { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
