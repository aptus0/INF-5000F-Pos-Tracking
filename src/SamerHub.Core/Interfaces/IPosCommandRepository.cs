using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPosCommandRepository
{
    Task<IReadOnlyList<PosCommand>> GetAllAsync(CancellationToken cancellationToken);
    Task<PosCommand?> GetNextPendingAsync(CancellationToken cancellationToken);
    Task<PosCommand> EnqueueAsync(PosCommand command, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid commandId, bool isSuccessful, CancellationToken cancellationToken);
}
