using Microsoft.EntityFrameworkCore;
using SamerHub.Core.Entities;
using SamerHub.Core.Enums;
using SamerHub.Core.Interfaces;

namespace SamerHub.Infrastructure.Persistence;

public sealed class EfPosCommandRepository(SamerHubDbContext dbContext) : IPosCommandRepository
{
    public async Task<IReadOnlyList<PosCommand>> GetAllAsync(CancellationToken cancellationToken) =>
        (await dbContext.PosCommands.ToListAsync(cancellationToken))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

    public async Task<PosCommand?> GetNextPendingAsync(CancellationToken cancellationToken) =>
        (await dbContext.PosCommands
            .Where(x => x.Status == PosCommandStatus.Pending)
            .ToListAsync(cancellationToken))
        .OrderBy(x => x.CreatedAtUtc)
        .FirstOrDefault();

    public async Task<PosCommand> EnqueueAsync(PosCommand command, CancellationToken cancellationToken)
    {
        if (command.Id == Guid.Empty)
        {
            command.Id = Guid.NewGuid();
        }

        dbContext.PosCommands.Add(command);
        await dbContext.SaveChangesAsync(cancellationToken);
        return command;
    }

    public async Task<bool> CompleteAsync(Guid commandId, bool isSuccessful, CancellationToken cancellationToken)
    {
        var command = await dbContext.PosCommands.SingleOrDefaultAsync(x => x.Id == commandId, cancellationToken);
        if (command is null)
        {
            return false;
        }

        command.Status = isSuccessful ? PosCommandStatus.Succeeded : PosCommandStatus.Failed;
        command.ResultCode = isSuccessful ? "00" : "99";
        command.ResultMessage = isSuccessful ? "APPROVED" : "FAILED";
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
