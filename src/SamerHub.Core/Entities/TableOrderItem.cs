namespace SamerHub.Core.Entities;

public sealed class TableOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableSessionId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool SentToKitchen { get; set; }
}
