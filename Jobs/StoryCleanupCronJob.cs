using MiniInstagram.Services;

namespace MiniInstagram.Jobs;

public class StoryCleanupCronJob(
    IServiceScopeFactory scopeFactory,
    ILogger<StoryCleanupCronJob> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var stories = scope.ServiceProvider.GetRequiredService<IStoryService>();
            var deletedCount = await stories.DeleteExpiredStoriesAsync(stoppingToken);

            if (deletedCount > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} expired stories.", deletedCount);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // App is shutting down.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Expired story cleanup failed.");
        }
    }
}
