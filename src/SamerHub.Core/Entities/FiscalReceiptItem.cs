namespace SamerHub.Core.Entities;

public sealed class FiscalReceiptItem
{
    public Guid Id { get; set; }
    public Guid FiscalReceiptId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatRate { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
}
