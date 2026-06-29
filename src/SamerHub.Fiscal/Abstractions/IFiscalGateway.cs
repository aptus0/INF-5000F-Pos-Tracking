namespace SamerHub.Fiscal.Abstractions;

using Models;

public interface IFiscalGateway
{
    Task<FiscalReceiptResult> CreateSaleReceiptAsync(FiscalReceiptRequest request, CancellationToken ct);
    Task<FiscalReceiptResult> CancelReceiptAsync(FiscalCancelRequest request, CancellationToken ct);
    Task<FiscalReceiptResult> RefundReceiptAsync(FiscalRefundRequest request, CancellationToken ct);
}
