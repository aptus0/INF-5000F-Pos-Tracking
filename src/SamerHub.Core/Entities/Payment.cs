using SamerHub.Core.Enums;

namespace SamerHub.Core.Entities;

public sealed class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TableSessionId { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public bool IsRefund { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
