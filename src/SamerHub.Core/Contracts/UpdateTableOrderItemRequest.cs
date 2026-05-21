namespace SamerHub.Core.Contracts;

public sealed class UpdateTableOrderItemRequest
{
    public int Quantity { get; set; } = 1;
    public string Note { get; set; } = string.Empty;
}
