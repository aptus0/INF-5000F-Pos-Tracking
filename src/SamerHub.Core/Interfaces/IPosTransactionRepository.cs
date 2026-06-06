using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPosTransactionRepository
{
    Task<IReadOnlyList<PosTransaction>> GetAllAsync(CancellationToken cancellationToken);
    Task<PosTransaction> AddAsync(PosTransaction transaction, CancellationToken cancellationToken);
}
