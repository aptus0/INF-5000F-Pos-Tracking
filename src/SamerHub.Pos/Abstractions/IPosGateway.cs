namespace SamerHub.Pos.Abstractions;

public interface IPosGateway
{
    Task<PosSaleResult> SaleAsync(PosSaleRequest request, CancellationToken cancellationToken);
    Task<PosVoidResult> VoidAsync(PosVoidRequest request, CancellationToken cancellationToken);
    Task<PosRefundResult> RefundAsync(PosRefundRequest request, CancellationToken cancellationToken);
    Task<PosStatusResult> GetStatusAsync(string referenceNumber, CancellationToken cancellationToken);
}
