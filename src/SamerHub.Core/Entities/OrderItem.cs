namespace SamerHub.Core.Entities;

public sealed class OrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string Note { get; set; } = string.Empty;
    public decimal VatRate { get; set; } = 0;
    public string DepartmentCode { get; set; } = string.Empty;
}
