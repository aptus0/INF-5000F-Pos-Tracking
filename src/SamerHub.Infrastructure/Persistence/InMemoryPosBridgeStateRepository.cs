using SamerHub.Core.Contracts;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPosBridgeStateRepository : IPosBridgeStateRepository
{
    private PosBridgeStatusResponse? _state;

    public Task<PosBridgeStatusResponse?> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(_state);

    public Task<PosBridgeStatusResponse> UpdateAsync(PosBridgeStatusUpdateRequest request, CancellationToken cancellationToken)
    {
        _state = new PosBridgeStatusResponse
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

        return Task.FromResult(_state);
    }
}
