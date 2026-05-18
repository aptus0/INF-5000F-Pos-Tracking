namespace SamerHub.Core.Contracts;

public sealed class PosTransactionResultRequest
{
    public Guid CommandId { get; set; }
    public bool IsSuccessful { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}
