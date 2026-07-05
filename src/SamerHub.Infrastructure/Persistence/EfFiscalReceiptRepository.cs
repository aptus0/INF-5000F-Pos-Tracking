using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfFiscalReceiptRepository(SamerHubDbContext dbContext) : IFiscalReceiptRepository
{
    public async Task<IReadOnlyList<FiscalReceipt>> GetAllAsync(CancellationToken ct) =>
        (await dbContext.FiscalReceipts
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .ToListAsync(ct))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

    public Task<FiscalReceipt?> GetByOrderIdAsync(Guid orderId, CancellationToken ct) =>
        dbContext.FiscalReceipts
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.OrderId == orderId, ct);

    public async Task<FiscalReceipt> AddAsync(FiscalReceipt receipt, CancellationToken ct)
    {
        if (receipt.Id == Guid.Empty)
        {
            receipt.Id = Guid.NewGuid();
        }

        receipt.CreatedAtUtc = DateTimeOffset.UtcNow;
        dbContext.FiscalReceipts.Add(receipt);
        await dbContext.SaveChangesAsync(ct);
        return receipt;
    }

    public async Task<FiscalReceipt> UpdateStatusAsync(Guid id, FiscalReceiptStatus status, string receiptNo, string rawResponse, CancellationToken ct)
    {
        var receipt = await dbContext.FiscalReceipts.SingleAsync(x => x.Id == id, ct);
        receipt.Status = status;
        receipt.ReceiptNo = receiptNo;
        receipt.RawResponse = rawResponse;
        await dbContext.SaveChangesAsync(ct);
        return receipt;
    }
}
