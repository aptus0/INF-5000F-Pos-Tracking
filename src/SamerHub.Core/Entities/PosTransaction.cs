namespace SamerHub.Core.Entities;

public sealed class PosTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PosCommandId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? TableSessionId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Stan { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ApprovalCode { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
