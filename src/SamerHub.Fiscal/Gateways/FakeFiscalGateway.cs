namespace SamerHub.Fiscal.Gateways;

using Abstractions;
using Models;

public sealed class FakeFiscalGateway : IFiscalGateway
{
    public Task<FiscalReceiptResult> CreateSaleReceiptAsync(FiscalReceiptRequest request, CancellationToken ct)
    {
        var result = new FiscalReceiptResult
        {
            IsSuccessful = true,
            ReceiptNo = $"FAKE-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            ZNo = "Z001",
            EcrNo = "ECR001",
            TotalVat = request.Items.Sum(i => (i.UnitPrice * i.Quantity * i.VatRate) / 100),
            RawResponse = "Fake success response",
            ErrorCode = string.Empty,
            ErrorMessage = string.Empty
        };

        return Task.FromResult(result);
    }

    public Task<FiscalReceiptResult> CancelReceiptAsync(FiscalCancelRequest request, CancellationToken ct)
    {
        var result = new FiscalReceiptResult
        {
            IsSuccessful = true,
            ReceiptNo = request.OriginalReceiptNo,
            ZNo = "Z001",
            EcrNo = "ECR001",
            RawResponse = "Fake cancel success",
            ErrorCode = string.Empty,
            ErrorMessage = string.Empty
        };

        return Task.FromResult(result);
    }

    public Task<FiscalReceiptResult> RefundReceiptAsync(FiscalRefundRequest request, CancellationToken ct)
    {
        var result = new FiscalReceiptResult
        {
            IsSuccessful = true,
            ReceiptNo = $"FAKE-REFUND-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            ZNo = "Z001",
            EcrNo = "ECR001",
            TotalVat = request.Items.Sum(i => (i.UnitPrice * i.Quantity * i.VatRate) / 100),
            RawResponse = "Fake refund success",
            ErrorCode = string.Empty,
            ErrorMessage = string.Empty
        };

        return Task.FromResult(result);
    }
}
