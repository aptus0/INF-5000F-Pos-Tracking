using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPosTransactionRepository : IPosTransactionRepository
{
    private readonly List<PosTransaction> _transactions = [];

    public Task<IReadOnlyList<PosTransaction>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PosTransaction>>(_transactions.OrderByDescending(x => x.ProcessedAtUtc).ToList());

    public Task<PosTransaction> AddAsync(PosTransaction transaction, CancellationToken cancellationToken)
    {
        _transactions.Add(transaction);
        return Task.FromResult(transaction);
    }
}
