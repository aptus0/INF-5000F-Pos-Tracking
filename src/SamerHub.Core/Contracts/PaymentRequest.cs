using SamerHub.Core.Enums;

namespace SamerHub.Core.Contracts;

public sealed class PaymentRequest
{
    public Guid TableSessionId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}
