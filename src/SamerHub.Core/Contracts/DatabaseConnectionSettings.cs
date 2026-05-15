namespace SamerHub.Core.Contracts;

public sealed class DatabaseConnectionSettings
{
    public string Provider { get; set; } = "Sqlite";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3306;
    public string Database { get; set; } = "samerhub";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
}
