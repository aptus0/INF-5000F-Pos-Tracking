namespace SamerHub.Fiscal.Gateways;

using Abstractions;
using Microsoft.Extensions.Logging;
using Models;

public sealed class IngenicoEcrFiscalGateway : IFiscalGateway
{
    private readonly ILogger<IngenicoEcrFiscalGateway> _logger;

    public IngenicoEcrFiscalGateway(ILogger<IngenicoEcrFiscalGateway> logger)
    {
        _logger = logger;
    }

    public Task<FiscalReceiptResult> CreateSaleReceiptAsync(FiscalReceiptRequest request, CancellationToken ct)
    {
        _logger.LogWarning("Ingenico ECR fiscal receipt creation requested, but ECR protocol documentation is still pending.");

        var result = new FiscalReceiptResult
        {
            IsSuccessful = false,
            ErrorCode = "ECR_NOT_READY",
            ErrorMessage = "ECR fiscal protocol documentation pending. Cannot process request."
        };

        return Task.FromResult(result);
    }

    public Task<FiscalReceiptResult> CancelReceiptAsync(FiscalCancelRequest request, CancellationToken ct)
    {
        _logger.LogWarning("Ingenico ECR fiscal receipt cancel requested, but ECR protocol documentation is still pending.");

        var result = new FiscalReceiptResult
        {
            IsSuccessful = false,
            ErrorCode = "ECR_NOT_READY",
            ErrorMessage = "ECR fiscal protocol documentation pending. Cannot process request."
        };

        return Task.FromResult(result);
    }

    public Task<FiscalReceiptResult> RefundReceiptAsync(FiscalRefundRequest request, CancellationToken ct)
    {
        _logger.LogWarning("Ingenico ECR fiscal receipt refund requested, but ECR protocol documentation is still pending.");

        var result = new FiscalReceiptResult
        {
            IsSuccessful = false,
            ErrorCode = "ECR_NOT_READY",
            ErrorMessage = "ECR fiscal protocol documentation pending. Cannot process request."
        };

        return Task.FromResult(result);
    }
}
