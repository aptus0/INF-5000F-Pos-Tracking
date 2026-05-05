namespace SamerHub.Core.Entities;

public sealed class VatRate
{
    public Guid Id { get; set; }
    public decimal Rate { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
