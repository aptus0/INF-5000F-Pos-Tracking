using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPaymentRepository
{
    Task<IReadOnlyList<Payment>> GetBySessionIdAsync(Guid tableSessionId, CancellationToken cancellationToken);
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken);
}
