namespace SamerHub.Core.Entities;

public sealed class VatDepartmentMapping
{
    public decimal VatRate { get; set; }
    public string DepartmentCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
