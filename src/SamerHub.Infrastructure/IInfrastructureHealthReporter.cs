namespace SamerHub.Infrastructure;

public interface IInfrastructureHealthReporter
{
    Task<InfrastructureHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
