namespace SamerHub.Core.Interfaces;

using Entities;
using Enums;

public interface IFiscalReceiptRepository
{
    Task<IReadOnlyList<FiscalReceipt>> GetAllAsync(CancellationToken ct);
    Task<FiscalReceipt?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task<FiscalReceipt> AddAsync(FiscalReceipt receipt, CancellationToken ct);
    Task<FiscalReceipt> UpdateStatusAsync(Guid id, FiscalReceiptStatus status, string receiptNo, string rawResponse, CancellationToken ct);
}
