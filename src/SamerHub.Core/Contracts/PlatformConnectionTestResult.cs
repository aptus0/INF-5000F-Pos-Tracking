namespace SamerHub.Core.Contracts;

public sealed class PlatformConnectionTestResult
{
    public string PlatformName { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public int? HttpStatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset TestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
