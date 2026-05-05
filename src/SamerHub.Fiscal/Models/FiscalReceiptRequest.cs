namespace SamerHub.Fiscal.Models;

public sealed class FiscalReceiptRequest
{
    public Guid ReceiptId { get; set; }
    public string ReceiptType { get; set; } = "SALE";
    public string OrderNo { get; set; } = string.Empty;
    public IList<FiscalReceiptItemLine> Items { get; set; } = [];
    public IList<FiscalPaymentLine> Payments { get; set; } = [];
    public string CashierId { get; set; } = string.Empty;
    public string BranchId { get; set; } = string.Empty;
}

public sealed class FiscalReceiptItemLine
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
}

public sealed class FiscalPaymentLine
{
    public string PaymentCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
