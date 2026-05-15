namespace SamerHub.Core.Contracts;

public sealed class CreateTableRequest
{
    public string Name { get; set; } = string.Empty;
    public string Area { get; set; } = "Salon";
    public int Capacity { get; set; } = 4;
}
