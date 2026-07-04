namespace SamerHub.Infrastructure;

public sealed class InfrastructureHealthSnapshot
{
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string Description { get; set; } = string.Empty;
}
