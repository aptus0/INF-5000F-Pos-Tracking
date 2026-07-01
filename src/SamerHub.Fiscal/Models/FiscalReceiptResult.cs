namespace SamerHub.Fiscal.Models;

public sealed class FiscalReceiptResult
{
    public bool IsSuccessful { get; set; }
    public string ReceiptNo { get; set; } = string.Empty;
    public string ZNo { get; set; } = string.Empty;
    public string EcrNo { get; set; } = string.Empty;
    public decimal TotalVat { get; set; }
    public string RawResponse { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
