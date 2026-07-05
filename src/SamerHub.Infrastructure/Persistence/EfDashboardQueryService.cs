using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Contracts;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfDashboardQueryService(SamerHubDbContext dbContext) : IDashboardQueryService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var sessions = await dbContext.TableSessions
            .Include(x => x.Payments)
            .Where(x => x.OpenedAtUtc >= today)
            .ToListAsync(cancellationToken);

        var payments = sessions.SelectMany(x => x.Payments).Where(x => x.CreatedAtUtc >= today).ToList();
        var orders = (await dbContext.Orders.ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .ToList();

        return new DashboardSummaryResponse
        {
            TodayRevenue = payments.Sum(x => x.Amount),
            CashTotal = payments.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Amount),
            CardTotal = payments.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.Amount),
            OnlineTotal = payments.Where(x => x.Method == PaymentMethod.Platform).Sum(x => x.Amount),
            OpenTables = await dbContext.DiningTables.CountAsync(x => x.IsOccupied, cancellationToken),
            PendingPackageOrders = await dbContext.Orders.CountAsync(x => x.Status == OrderStatus.New || x.Status == OrderStatus.Accepted, cancellationToken),
            KitchenInProgress = await dbContext.TableSessions.CountAsync(x => x.Status == TableSessionStatus.SentToKitchen, cancellationToken),
            RecentOrders = orders.Select(x => new RecentOrderSummary
            {
                Reference = x.ExternalOrderId,
                Source = x.PlatformName,
                TotalAmount = x.TotalAmount,
                Status = x.Status.ToString()
            }).ToList(),
            Alerts =
            [
                "Service live",
                await dbContext.DiningTables.AnyAsync(x => x.IsOccupied, cancellationToken) ? "Open table sessions active" : "No open tables"
            ]
        };
    }
}
