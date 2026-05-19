using SamerHub.Core.Enums;

namespace SamerHub.Core.Contracts;

public sealed class SplitPaymentRequest
{
    public Guid TableSessionId { get; set; }
    public List<SplitPaymentLine> Payments { get; set; } = [];
}

public sealed class SplitPaymentLine
{
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
}
