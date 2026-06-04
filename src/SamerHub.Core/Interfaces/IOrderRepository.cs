using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken cancellationToken);
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task<Order> AddManualAsync(Order order, CancellationToken cancellationToken);
    Task<bool> UpdateStatusAsync(Guid orderId, string action, CancellationToken cancellationToken);
}
