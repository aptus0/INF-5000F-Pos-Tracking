namespace SamerHub.Core.Contracts;

public sealed class ServiceHealthResponse
{
    public string Service { get; set; } = "SAMER Hub Service";
    public string Status { get; set; } = "Running";
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset Utc { get; set; } = DateTimeOffset.UtcNow;
}
