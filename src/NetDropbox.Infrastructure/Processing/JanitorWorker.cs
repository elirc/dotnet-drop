using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetDropbox.Application.Abstractions;
using NetDropbox.Domain;
using NetDropbox.Infrastructure.Persistence;

namespace NetDropbox.Infrastructure.Processing;

public sealed class JanitorWorker(IServiceScopeFactory scopeFactory, ILogger<JanitorWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Janitor sweep failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetDropboxDbContext>();
        var chunks = scope.ServiceProvider.GetRequiredService<IChunkStorage>();
        var now = DateTimeOffset.UtcNow;

        var expired = await db.UploadSessions
            .Where(x => x.Status == UploadSessionStatus.Open && x.ExpiresAt <= now)
            .ToArrayAsync(cancellationToken);
        foreach (var session in expired)
        {
            session.Expire();
            await chunks.DeleteSessionAsync(session.Id, cancellationToken);
        }

        var cutoff = now.AddDays(-30);
        var oldTrash = await db.FileNodes
            .Where(x => x.IsDeleted && x.DeletedAt <= cutoff)
            .ToArrayAsync(cancellationToken);
        db.FileNodes.RemoveRange(oldTrash);
        await db.SaveChangesAsync(cancellationToken);
    }
}

