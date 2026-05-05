using SamerHub.Core.Entities;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPosSettingsRepository : IPosSettingsRepository
{
    private PosSettings _settings = new();

    public Task<PosSettings> GetAsync(CancellationToken cancellationToken)
        => Task.FromResult(Clone(_settings));

    public Task<PosSettings> UpdateAsync(PosSettings settings, CancellationToken cancellationToken)
    {
        _settings = Clone(settings);
        return Task.FromResult(Clone(_settings));
    }

    public Task<PosSettings> UpdateConnectionResultAsync(
        bool isSuccessful,
        long? roundTripMilliseconds,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        _settings.LastConnectionSucceeded = isSuccessful;
        _settings.LastConnectionAtUtc = DateTimeOffset.UtcNow;
        _settings.LastRoundTripMilliseconds = roundTripMilliseconds;
        _settings.LastError = errorMessage;
        return Task.FromResult(Clone(_settings));
    }

    private static PosSettings Clone(PosSettings settings)
    {
        return new PosSettings
        {
            IsEnabled = settings.IsEnabled,
            Mode = settings.Mode,
            PcIpAddress = settings.PcIpAddress,
            PosIpAddress = settings.PosIpAddress,
            PosPort = settings.PosPort,
            TimeoutSeconds = settings.TimeoutSeconds,
            CurrencyCode = settings.CurrencyCode,
            CurrencyNumericCode = settings.CurrencyNumericCode,
            DefaultPaymentMethod = settings.DefaultPaymentMethod,
            MinimumTestAmount = settings.MinimumTestAmount,
            TerminalId = settings.TerminalId,
            MerchantId = settings.MerchantId,
            EcrSecret = settings.EcrSecret,
            Protocol = settings.Protocol,
            LastConnectionSucceeded = settings.LastConnectionSucceeded,
            LastConnectionAtUtc = settings.LastConnectionAtUtc,
            LastRoundTripMilliseconds = settings.LastRoundTripMilliseconds,
            LastError = settings.LastError
        };
    }
}
