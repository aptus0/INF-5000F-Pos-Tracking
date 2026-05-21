namespace SamerHub.Core.Contracts;

public sealed class UpdateTableRequest
{
    public string Name { get; set; } = string.Empty;
    public string Area { get; set; } = "Salon";
    public int Capacity { get; set; } = 4;
    public string Status { get; set; } = "Available";
}
