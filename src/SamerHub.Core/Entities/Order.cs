using SamerHub.Core.Enums;

namespace SamerHub.Core.Entities;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ExternalOrderId { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.New;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
