using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfTableSessionRepository(SamerHubDbContext dbContext) : ITableSessionRepository
{
    public Task<TableSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.TableSessions
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TableSession>> GetOpenSessionsAsync(CancellationToken cancellationToken) =>
        (await dbContext.TableSessions
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .Where(x => x.Status != TableSessionStatus.Closed && x.Status != TableSessionStatus.Cancelled)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.OpenedAtUtc)
            .ToList();

    public async Task<TableSession> OpenAsync(Guid tableId, string tableName, CancellationToken cancellationToken)
    {
        var existing = await dbContext.TableSessions
            .FirstOrDefaultAsync(x => x.TableId == tableId && x.Status != TableSessionStatus.Closed && x.Status != TableSessionStatus.Cancelled, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var session = new TableSession
        {
            TableId = tableId,
            TableName = tableName,
            Status = TableSessionStatus.Open
        };

        dbContext.TableSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<TableSession?> CloseAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var session = await dbContext.TableSessions
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.TableId == tableId && x.Status != TableSessionStatus.Closed && x.Status != TableSessionStatus.Cancelled, cancellationToken);

        if (session is null)
        {
            return null;
        }

        session.Status = TableSessionStatus.Closed;
        session.ClosedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<TableSession> SaveAsync(TableSession session, CancellationToken cancellationToken)
    {
        dbContext.TableSessions.Update(session);
        await dbContext.SaveChangesAsync(cancellationToken);
        return session;
    }
}
