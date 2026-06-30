namespace SamerHub.Fiscal.Models;

public sealed class FiscalCancelRequest
{
    public Guid ReceiptId { get; set; }
    public string OriginalReceiptNo { get; set; } = string.Empty;
}
