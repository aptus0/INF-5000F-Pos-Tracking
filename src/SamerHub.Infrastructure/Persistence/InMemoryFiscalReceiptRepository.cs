using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryFiscalReceiptRepository : IFiscalReceiptRepository
{
    private readonly List<FiscalReceipt> _receipts = [];

    public Task<IReadOnlyList<FiscalReceipt>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<FiscalReceipt>>(_receipts.OrderByDescending(r => r.CreatedAtUtc).ToList());

    public Task<FiscalReceipt?> GetByOrderIdAsync(Guid orderId, CancellationToken ct)
        => Task.FromResult(_receipts.FirstOrDefault(r => r.OrderId == orderId));

    public Task<FiscalReceipt> AddAsync(FiscalReceipt receipt, CancellationToken ct)
    {
        if (receipt.Id == Guid.Empty)
        {
            receipt.Id = Guid.NewGuid();
        }
        receipt.CreatedAtUtc = DateTimeOffset.UtcNow;
        _receipts.Add(receipt);
        return Task.FromResult(receipt);
    }

    public Task<FiscalReceipt> UpdateStatusAsync(Guid id, FiscalReceiptStatus status, string receiptNo, string rawResponse, CancellationToken ct)
    {
        var receipt = _receipts.FirstOrDefault(r => r.Id == id);
        if (receipt is not null)
        {
            receipt.Status = status;
            receipt.ReceiptNo = receiptNo;
            receipt.RawResponse = rawResponse;
        }
        return Task.FromResult(receipt!);
    }
}
