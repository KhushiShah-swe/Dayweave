using Microsoft.EntityFrameworkCore;
using PersonalTimeline.API.Services;
namespace PersonalTimeline.API.BackgroundServices;

public sealed class SyncWorker(IServiceScopeFactory scopeFactory, SyncCoordinator coordinator, ILogger<SyncWorker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Sync:Enabled", true)) return;
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        do
        {
            try { await SyncOnce(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning("Sync cycle failed ({ErrorType}); the next cycle will retry.", ex.GetType().Name); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task SyncOnce(CancellationToken cancellationToken)
    {
        List<int> ids;
        using (var scope = scopeFactory.CreateScope())
            ids = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ApiConnections
                .Where(c => c.IsActive).Select(c => c.Id).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var lease = await coordinator.EnterAsync(cancellationToken);
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = await db.ApiConnections.FindAsync(new object[] { id }, cancellationToken);
            if (connection is null || !connection.IsActive) continue;
            var service = scope.ServiceProvider.GetServices<IThirdPartyApiService>().SingleOrDefault(s => s.ProviderName == connection.ApiProvider);
            if (service is null) continue;
            try
            {
                await service.SyncUserDataAsync(db, connection, connection.UserId);
                connection.LastSyncAt = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { logger.LogWarning("Provider {Provider} sync failed ({ErrorType}).", connection.ApiProvider, ex.GetType().Name); }
        }
    }
}
