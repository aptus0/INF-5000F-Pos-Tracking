using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPaymentRepository(SamerHubDbContext dbContext) : IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetBySessionIdAsync(Guid tableSessionId, CancellationToken cancellationToken) =>
        (await dbContext.Payments
            .Where(x => x.TableSessionId == tableSessionId)
            .ToListAsync(cancellationToken))
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return payment;
    }
}
