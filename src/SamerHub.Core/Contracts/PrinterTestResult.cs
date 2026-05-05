namespace SamerHub.Core.Contracts;

public sealed class PrinterTestResult
{
    public bool IsSuccessful { get; set; }
    public string PrinterName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<string> InstalledPrinters { get; set; } = [];
}
