namespace SamerHub.Core.Entities;

public sealed class DiningTable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Area { get; set; } = "Salon";
    public int Capacity { get; set; } = 4;
    public bool IsOccupied { get; set; }
    public string Status { get; set; } = "Available";
    public decimal ActiveOrderTotal { get; set; }
}
