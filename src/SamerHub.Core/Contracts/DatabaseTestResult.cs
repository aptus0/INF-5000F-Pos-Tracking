namespace SamerHub.Core.Contracts;

public sealed class DatabaseTestResult
{
    public string Provider { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string Message { get; set; } = string.Empty;
}
