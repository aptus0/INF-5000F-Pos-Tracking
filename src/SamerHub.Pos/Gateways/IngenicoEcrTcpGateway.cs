using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using SamerHub.Core.Interfaces;
using SamerHub.Pos.Abstractions;

namespace SamerHub.Pos.Gateways;

public sealed class IngenicoEcrTcpGateway(
    IPosSettingsRepository settingsRepository,
    ILogger<IngenicoEcrTcpGateway> logger) : IPosGateway
{
    public async Task<PosSaleResult> SaleAsync(PosSaleRequest request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (!settings.IsEnabled)
        {
            return new PosSaleResult
            {
                IsSuccessful = false,
                ErrorMessage = "POS entegrasyonu pasif."
            };
        }

        var connectionResult = await OpenConnectionAsync(settings, cancellationToken);
        await settingsRepository.UpdateConnectionResultAsync(
            connectionResult.IsReachable,
            connectionResult.RoundTripMilliseconds,
            connectionResult.ErrorMessage,
            cancellationToken);

        if (!connectionResult.IsReachable)
        {
            return new PosSaleResult
            {
                IsSuccessful = false,
                ErrorCode = connectionResult.ErrorCode,
                ErrorMessage = connectionResult.ErrorMessage,
                RawResponse = connectionResult.RawResponse
            };
        }

        logger.LogWarning(
            "TCP connection to POS {Ip}:{Port} succeeded for order {OrderNumber}, but SALE frame was not sent because ECR protocol documentation is still required.",
            settings.PosIpAddress,
            settings.PosPort,
            request.OrderNumber);

        return new PosSaleResult
        {
            IsSuccessful = false,
            RawResponse = "TCP connection successful. SALE frame was not sent because ECR protocol format is not yet implemented.",
            ErrorMessage = "POS baglantisi kuruldu ancak SALE mesaji ECR protokol dokumani olmadan gonderilmedi."
        };
    }

    public async Task<PosStatusResult> GetStatusAsync(string referenceNumber, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        var result = await OpenConnectionAsync(settings, cancellationToken, referenceNumber);

        await settingsRepository.UpdateConnectionResultAsync(
            result.IsReachable,
            result.RoundTripMilliseconds,
            result.ErrorMessage,
            cancellationToken);

        return result;
    }

    public Task<PosVoidResult> VoidAsync(PosVoidRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PosVoidResult
        {
            IsSuccessful = false,
            ErrorMessage = "VOID icin ECR protokol format dokumani bekleniyor."
        });
    }

    public Task<PosRefundResult> RefundAsync(PosRefundRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new PosRefundResult
        {
            IsSuccessful = false,
            ErrorMessage = "REFUND icin ECR protokol format dokumani bekleniyor."
        });
    }

    private async Task<PosStatusResult> OpenConnectionAsync(
        SamerHub.Core.Entities.PosSettings settings,
        CancellationToken cancellationToken,
        string referenceNumber = "")
    {
        if (!settings.IsEnabled)
        {
            return new PosStatusResult
            {
                IsReachable = false,
                ReferenceNumber = referenceNumber,
                TerminalId = settings.TerminalId,
                ErrorMessage = "POS entegrasyonu pasif.",
                RawResponse = "POS integration disabled"
            };
        }

        if (!settings.Mode.Equals("IngenicoEcrTcp", StringComparison.OrdinalIgnoreCase))
        {
            return new PosStatusResult
            {
                IsReachable = false,
                ReferenceNumber = referenceNumber,
                TerminalId = settings.TerminalId,
                ErrorMessage = $"Aktif POS modu '{settings.Mode}'. Bu surumde yalnizca IngenicoEcrTcp desteklenir.",
                RawResponse = "Gateway mode mismatch"
            };
        }

        try
        {
            using var client = new TcpClient();
            var sw = Stopwatch.StartNew();
            await client.ConnectAsync(settings.PosIpAddress, settings.PosPort, cancellationToken)
                .AsTask()
                .WaitAsync(TimeSpan.FromSeconds(settings.TimeoutSeconds), cancellationToken);
            sw.Stop();

            logger.LogInformation(
                "TCP connection to POS succeeded. POS {Ip}:{Port}, elapsed {Elapsed} ms",
                settings.PosIpAddress,
                settings.PosPort,
                sw.ElapsedMilliseconds);

            return new PosStatusResult
            {
                IsReachable = true,
                ReferenceNumber = referenceNumber,
                TerminalId = settings.TerminalId,
                RawResponse = "TCP connection successful",
                RoundTripMilliseconds = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "TCP connection to POS failed. POS {Ip}:{Port}",
                settings.PosIpAddress,
                settings.PosPort);

            return new PosStatusResult
            {
                IsReachable = false,
                ReferenceNumber = referenceNumber,
                TerminalId = settings.TerminalId,
                RawResponse = ex.Message,
                ErrorCode = "TCP_CONNECT_FAILED",
                ErrorMessage = ex.Message
            };
        }
    }
}
