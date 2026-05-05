using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfOrderRepository(SamerHubDbContext dbContext) : IOrderRepository
{
    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken) =>
        (await dbContext.Orders
            .Include(x => x.Items)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

    public async Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken cancellationToken) =>
        (await dbContext.Orders
            .Include(x => x.Items)
            .Where(x => x.Status == OrderStatus.New)
            .ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

    public Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken) =>
        dbContext.Orders.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);

    public async Task<Order> AddManualAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Id == Guid.Empty)
        {
            order.Id = Guid.NewGuid();
        }

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<bool> UpdateStatusAsync(Guid orderId, string action, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken);
        if (order is null)
        {
            return false;
        }

        order.Status = action.ToLowerInvariant() switch
        {
            "accept" => OrderStatus.Accepted,
            "ready" => OrderStatus.Ready,
            _ => order.Status
        };

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
