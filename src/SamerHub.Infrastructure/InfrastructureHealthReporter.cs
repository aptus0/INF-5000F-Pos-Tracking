using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SamerHub.Infrastructure.Persistence;

namespace SamerHub.Infrastructure;

public sealed class InfrastructureHealthReporter(
    SamerHubDbContext dbContext,
    IOptions<DatabaseOptions> databaseOptions) : IInfrastructureHealthReporter
{
    public async Task<InfrastructureHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        return new InfrastructureHealthSnapshot
        {
            Provider = databaseOptions.Value.Provider,
            IsConnected = canConnect,
            Description = canConnect
                ? "Database connection is healthy."
                : "Database connection could not be established."
        };
    }
}
