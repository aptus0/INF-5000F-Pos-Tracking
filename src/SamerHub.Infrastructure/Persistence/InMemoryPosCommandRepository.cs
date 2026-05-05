using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class InMemoryPosCommandRepository : IPosCommandRepository
{
    private readonly List<PosCommand> _commands = [];

    public Task<IReadOnlyList<PosCommand>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PosCommand>>(_commands.OrderByDescending(x => x.CreatedAtUtc).ToList());

    public Task<PosCommand?> GetNextPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult(_commands.FirstOrDefault(x => x.Status == PosCommandStatus.Pending));

    public Task<PosCommand> EnqueueAsync(PosCommand command, CancellationToken cancellationToken)
    {
        _commands.Add(command);
        return Task.FromResult(command);
    }

    public Task<bool> CompleteAsync(Guid commandId, bool isSuccessful, CancellationToken cancellationToken)
    {
        var command = _commands.FirstOrDefault(x => x.Id == commandId);
        if (command is null)
        {
            return Task.FromResult(false);
        }

        command.Status = isSuccessful ? PosCommandStatus.Succeeded : PosCommandStatus.Failed;
        command.ResultCode = isSuccessful ? "00" : "99";
        command.ResultMessage = isSuccessful ? "SIMULATED_APPROVED" : "SIMULATED_FAILED";
        return Task.FromResult(true);
    }
}
