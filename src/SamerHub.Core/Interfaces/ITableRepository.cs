using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface ITableRepository
{
    Task<IReadOnlyList<DiningTable>> GetAllAsync(CancellationToken cancellationToken);
    Task<DiningTable?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DiningTable> AddAsync(DiningTable table, CancellationToken cancellationToken);
    Task<DiningTable> UpdateAsync(DiningTable table, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
