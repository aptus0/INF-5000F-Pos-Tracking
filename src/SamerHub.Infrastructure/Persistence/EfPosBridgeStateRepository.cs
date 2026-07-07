using SamerHub.Core.Contracts;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPosBridgeStateRepository(JsonSettingsStore store) : IPosBridgeStateRepository
{
    private const string Key = "status.posBridge";

    public async Task<PosBridgeStatusResponse?> GetAsync(CancellationToken cancellationToken) =>
        await store.GetAsync<PosBridgeStatusResponse?>(Key, () => null, cancellationToken);

    public async Task<PosBridgeStatusResponse> UpdateAsync(PosBridgeStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = new PosBridgeStatusResponse
        {
            TerminalId = request.TerminalId,
            TerminalName = request.TerminalName,
            AppVersion = request.AppVersion,
            LanIpAddress = request.LanIpAddress,
            IsConnectedToHub = request.IsConnectedToHub,
            IsApiSubscribed = request.IsApiSubscribed,
            IsTransactionServiceConnected = request.IsTransactionServiceConnected,
            IsReady = request.IsReady,
            TokenExpiresAtUtc = request.TokenExpiresAtUtc,
            LastSeenAtUtc = DateTimeOffset.UtcNow,
            LastError = request.LastError,
            AvailableCurrencies = request.AvailableCurrencies,
            AvailableTransactionTypes = request.AvailableTransactionTypes,
            AvailablePaymentMeans = request.AvailablePaymentMeans
        };

        await store.SaveAsync(Key, response, cancellationToken);
        return response;
    }
}
