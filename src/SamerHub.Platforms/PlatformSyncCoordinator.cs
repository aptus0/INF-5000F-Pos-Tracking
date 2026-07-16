namespace SamerHub.Platforms;

public sealed class PlatformSyncCoordinator
{
    public Task PollAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
