using SamerHub.Core.Contracts;

namespace SamerHub.Core.Interfaces;

public interface IPosBridgeStateRepository
{
    Task<PosBridgeStatusResponse?> GetAsync(CancellationToken cancellationToken);
    Task<PosBridgeStatusResponse> UpdateAsync(PosBridgeStatusUpdateRequest request, CancellationToken cancellationToken);
}
