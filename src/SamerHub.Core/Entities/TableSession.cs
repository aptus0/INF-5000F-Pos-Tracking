using SamerHub.Core.Enums;

namespace SamerHub.Core.Entities;

public sealed class TableSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public TableSessionStatus Status { get; set; } = TableSessionStatus.Open;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public List<TableOrderItem> Items { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
}
