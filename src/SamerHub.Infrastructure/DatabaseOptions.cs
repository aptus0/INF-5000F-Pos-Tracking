namespace SamerHub.Infrastructure;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; set; } = "Sqlite";
    public string ConnectionString { get; set; } = "Data Source=C:\\ProgramData\\SAMER Hub\\samerhub.db";
}
