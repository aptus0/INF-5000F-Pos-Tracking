using SamerHub.Core.Entities;

namespace SamerHub.Core.Interfaces;

public interface IPlatformSettingsRepository
{
    Task<PlatformApiSettings> GetAsync(CancellationToken cancellationToken);
    Task<PlatformApiSettings> UpdateAsync(PlatformApiSettings settings, CancellationToken cancellationToken);
}
