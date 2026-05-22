namespace SamerHub.Core.Entities;

public sealed class FiscalDepartment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PosCode { get; set; } = string.Empty;
    public decimal DefaultVatRate { get; set; }
    public string Description { get; set; } = string.Empty;
}
