using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly List<Order> _orders = [];

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Order>>(_orders.OrderByDescending(x => x.CreatedAtUtc).ToList());

    public Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<Order>>(_orders.Where(x => x.Status == OrderStatus.New).ToList());

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken)
        => Task.FromResult(_orders.FirstOrDefault(x => x.Id == orderId));

    public Task<Order> AddManualAsync(Order order, CancellationToken cancellationToken)
    {
        _orders.Add(order);
        return Task.FromResult(order);
    }

    public Task<bool> UpdateStatusAsync(Guid orderId, string action, CancellationToken cancellationToken)
    {
        var order = _orders.FirstOrDefault(x => x.Id == orderId);
        if (order is null)
        {
            return Task.FromResult(false);
        }

        order.Status = action.ToLowerInvariant() switch
        {
            "accept" => OrderStatus.Accepted,
            "ready" => OrderStatus.Ready,
            _ => order.Status
        };

        return Task.FromResult(true);
    }
}
