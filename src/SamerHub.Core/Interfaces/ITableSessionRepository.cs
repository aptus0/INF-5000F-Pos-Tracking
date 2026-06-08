using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface ITableSessionRepository
{
    Task<TableSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TableSession>> GetOpenSessionsAsync(CancellationToken cancellationToken);
    Task<TableSession> OpenAsync(Guid tableId, string tableName, CancellationToken cancellationToken);
    Task<TableSession?> CloseAsync(Guid tableId, CancellationToken cancellationToken);
    Task<TableSession> SaveAsync(TableSession session, CancellationToken cancellationToken);
}
