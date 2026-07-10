using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfTableRepository(SamerHubDbContext dbContext) : ITableRepository
{
    public async Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!await dbContext.DiningTables.AnyAsync(cancellationToken))
        {
            dbContext.DiningTables.AddRange(RepositoryDefaults.CreateTables());
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await dbContext.DiningTables.OrderBy(x => x.Area).ThenBy(x => x.Name).ToListAsync(cancellationToken);
    }

    public Task<DiningTable?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.DiningTables.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<DiningTable> AddAsync(DiningTable table, CancellationToken cancellationToken)
    {
        dbContext.DiningTables.Add(table);
        await dbContext.SaveChangesAsync(cancellationToken);
        return table;
    }

    public async Task<DiningTable> UpdateAsync(DiningTable table, CancellationToken cancellationToken)
    {
        dbContext.DiningTables.Update(table);
        await dbContext.SaveChangesAsync(cancellationToken);
        return table;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var table = await dbContext.DiningTables.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (table is null)
        {
            return;
        }

        dbContext.DiningTables.Remove(table);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
