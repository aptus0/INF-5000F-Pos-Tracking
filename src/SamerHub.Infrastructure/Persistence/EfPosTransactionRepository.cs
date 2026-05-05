using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPosTransactionRepository(SamerHubDbContext dbContext) : IPosTransactionRepository
{
    public async Task<IReadOnlyList<PosTransaction>> GetAllAsync(CancellationToken cancellationToken) =>
        (await dbContext.PosTransactions.ToListAsync(cancellationToken))
            .OrderByDescending(x => x.ProcessedAtUtc)
            .ToList();

    public async Task<PosTransaction> AddAsync(PosTransaction transaction, CancellationToken cancellationToken)
    {
        dbContext.PosTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);
        return transaction;
    }
}
