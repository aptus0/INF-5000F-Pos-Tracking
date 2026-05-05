namespace SamerHub.Core.Contracts;

public sealed class SystemStatusResponse
{
    public string ServiceName { get; set; } = "SAMER Hub Service";
    public string Version { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    public string ServiceUrl { get; set; } = string.Empty;
    public DatabaseStatusResponse Database { get; set; } = new();
    public IntegrationStatusResponse Pos { get; set; } = new();
    public IntegrationStatusResponse Fiscal { get; set; } = new();
    public IntegrationStatusResponse Printer { get; set; } = new();
    public IntegrationStatusResponse Internet { get; set; } = new();
    public IntegrationStatusResponse PlatformSync { get; set; } = new();
    public PortBindingStatusResponse PortBinding { get; set; } = new();
    public string LastError { get; set; } = string.Empty;
}

public sealed class DatabaseStatusResponse
{
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class IntegrationStatusResponse
{
    public bool Enabled { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class PortBindingStatusResponse
{
    public string Url { get; set; } = string.Empty;
    public bool IsPreferredPortReachable { get; set; }
    public bool IsCurrentInstanceServing { get; set; }
    public string Detail { get; set; } = string.Empty;
}
