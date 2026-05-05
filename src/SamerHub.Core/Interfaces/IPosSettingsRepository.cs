using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPosSettingsRepository
{
    Task<PosSettings> GetAsync(CancellationToken cancellationToken);
    Task<PosSettings> UpdateAsync(PosSettings settings, CancellationToken cancellationToken);
    Task<PosSettings> UpdateConnectionResultAsync(
        bool isSuccessful,
        long? roundTripMilliseconds,
        string errorMessage,
        CancellationToken cancellationToken);
}
