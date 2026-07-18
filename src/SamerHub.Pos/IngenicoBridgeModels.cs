namespace SamerHub.Pos;

public sealed class IngenicoSaleRequest
{
    public Guid CommandId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string OrderReference { get; set; } = string.Empty;
}

public sealed class IngenicoSaleResponse
{
    public Guid CommandId { get; set; }
    public bool IsSuccessful { get; set; }
    public string ResultCode { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}

public static class PosCapabilityNames
{
    public const string Sale = "SALE";
    public const string Void = "VOID";
    public const string Refund = "REFUND";
    public const string ContactChip = "CONTACT_CHIP";
    public const string Contactless = "CONTACTLESS";
    public const string Swipe = "SWIPE";
    public const string Qr = "QR";
    public const string TurkishLira = "TRY";
}
