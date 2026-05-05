using SamerHub.Core.Enums;

namespace SamerHub.Core.Entities;

public sealed class PosCommand
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public PosCommandType CommandType { get; set; } = PosCommandType.Sale;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string TerminalId { get; set; } = string.Empty;
    public PosCommandStatus Status { get; set; } = PosCommandStatus.Pending;
    public int RetryCount { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
