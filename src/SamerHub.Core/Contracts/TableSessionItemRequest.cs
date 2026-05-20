namespace SamerHub.Core.Contracts;

public sealed class TableSessionItemRequest
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public string Note { get; set; } = string.Empty;
}
