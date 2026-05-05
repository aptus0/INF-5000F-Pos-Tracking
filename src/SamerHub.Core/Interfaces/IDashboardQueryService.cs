using SamerHub.Core.Contracts;

namespace SamerHub.Core.Interfaces;

public interface IDashboardQueryService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
