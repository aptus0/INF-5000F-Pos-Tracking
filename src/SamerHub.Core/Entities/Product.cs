namespace SamerHub.Core.Entities;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public Guid VatRateId { get; set; }
    public int FiscalDepartmentId { get; set; }
    public decimal KdvOrani { get; set; }
    public string YnokcDepartmani { get; set; } = string.Empty;
    public string UnitType { get; set; } = "ADET";
    public string MaliUrunKodu { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
