namespace SamerHub.Pos.Gateways;

using Abstractions;

public sealed class FakePosGateway : IPosGateway
{
    public Task<PosSaleResult> SaleAsync(PosSaleRequest request, CancellationToken cancellationToken)
    {
        var result = new PosSaleResult
        {
            IsSuccessful = true,
            TransactionId = $"FAKE-{Guid.NewGuid():N}",
            Stan = "000001",
            ReceiptNumber = $"FAKE-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            ApprovalCode = "000001",
            RawResponse = "Fake POS sale success"
        };

        return Task.FromResult(result);
    }

    public Task<PosVoidResult> VoidAsync(PosVoidRequest request, CancellationToken cancellationToken)
    {
        var result = new PosVoidResult
        {
            IsSuccessful = true
        };

        return Task.FromResult(result);
    }

    public Task<PosRefundResult> RefundAsync(PosRefundRequest request, CancellationToken cancellationToken)
    {
        var result = new PosRefundResult
        {
            IsSuccessful = true
        };

        return Task.FromResult(result);
    }

    public Task<PosStatusResult> GetStatusAsync(string referenceNumber, CancellationToken cancellationToken)
    {
        var result = new PosStatusResult
        {
            IsReachable = true,
            ReferenceNumber = referenceNumber,
            RoundTripMilliseconds = 0
        };

        return Task.FromResult(result);
    }
}
