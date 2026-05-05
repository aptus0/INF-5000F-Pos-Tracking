namespace SamerHub.Pos;

public sealed class PosSaleRequest
{
    public string TerminalId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = PosCapabilityNames.TurkishLira;
    public int CurrencyNumericCode { get; set; } = 949;
}

public sealed class PosSaleResult
{
    public bool IsSuccessful { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string Stan { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ApprovalCode { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PosVoidRequest
{
    public string TerminalId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Stan { get; set; } = string.Empty;
}

public sealed class PosVoidResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PosRefundRequest
{
    public string TerminalId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Stan { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = PosCapabilityNames.TurkishLira;
    public int CurrencyNumericCode { get; set; } = 949;
}

public sealed class PosRefundResult
{
    public bool IsSuccessful { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class PosStatusResult
{
    public bool IsReachable { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Stan { get; set; } = string.Empty;
    public string ApprovalCode { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public long? RoundTripMilliseconds { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
