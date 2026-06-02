using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken);
}
