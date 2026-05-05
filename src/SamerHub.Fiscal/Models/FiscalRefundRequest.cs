namespace SamerHub.Fiscal.Models;

public sealed class FiscalRefundRequest
{
    public Guid ReceiptId { get; set; }
    public string OriginalReceiptNo { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public IList<FiscalReceiptItemLine> Items { get; set; } = [];
}
