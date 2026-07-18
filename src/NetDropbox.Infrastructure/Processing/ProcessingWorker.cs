using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetDropbox.Application.Abstractions;
using NetDropbox.Domain;
using NetDropbox.Infrastructure.Persistence;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace NetDropbox.Infrastructure.Processing;

public sealed class ProcessingWorker(IServiceScopeFactory scopeFactory, IProcessingQueue queue, ILogger<ProcessingWorker> logger) : BackgroundService
{
    private const string EicarSignature = "EICAR-STANDARD-ANTIVIRUS-TEST-FILE";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.DequeueAsync(stoppingToken))
        {
            await ProcessAsync(message, stoppingToken);
        }
    }

    private async Task ProcessAsync(ProcessingMessage message, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetDropboxDbContext>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorage>();

        var job = await db.ProcessingJobs.SingleOrDefaultAsync(x => x.Id == message.JobId, cancellationToken);
        var node = await db.FileNodes.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == message.FileNodeId, cancellationToken);
        var version = await db.FileVersions.SingleOrDefaultAsync(x => x.Id == message.FileVersionId, cancellationToken);
        if (job is null || node is null || version is null) return;

        try
        {
            job.MarkRunning();
            await db.SaveChangesAsync(cancellationToken);

            var blob = await db.Blobs.SingleAsync(x => x.Id == version.BlobId, cancellationToken);
            await using var stream = await blobStorage.OpenReadAsync(blob.StorageKey, cancellationToken);
            var sampleText = await ReadTextSampleAsync(stream, cancellationToken);
            if (sampleText.Contains(EicarSignature, StringComparison.OrdinalIgnoreCase))
            {
                node.MarkInfected();
                db.ScanResults.Add(ScanResult.Create(node.Id, version.Id, FileScanStatus.Infected, "simulated-eicar", EicarSignature));
                job.MarkCompleted();
                logger.LogWarning("Upload {FileNodeId} was quarantined by scanner", node.Id);
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            string? extracted = IsText(version.MimeType) ? sampleText : null;
            string? thumbnail = null;
            if (version.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                thumbnail = await GenerateThumbnailAsync(blobStorage, blob.StorageKey, node.Id, cancellationToken);
            }

            node.MarkScanClean(extracted, thumbnail);
            db.ScanResults.Add(ScanResult.Create(node.Id, version.Id, FileScanStatus.Clean, "simulated-eicar", null));
            job.MarkCompleted();
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing job {JobId} failed", message.JobId);
            job.MarkFailed(ex.Message);
            node.MarkScanFailed();
            await db.SaveChangesAsync(cancellationToken);
            if (job.Status != ProcessingJobStatus.DeadLettered)
                await queue.EnqueueAsync(message, cancellationToken);
        }
    }

    private static async Task<string> ReadTextSampleAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek) stream.Position = 0;
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        var remaining = 1024 * 1024;
        while (remaining > 0)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, remaining)), cancellationToken);
            if (read == 0) break;
            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            remaining -= read;
        }
        return Encoding.UTF8.GetString(memory.ToArray());
    }

    private static async Task<string?> GenerateThumbnailAsync(IBlobStorage blobStorage, string storageKey, Guid nodeId, CancellationToken cancellationToken)
    {
        await using var imageStream = await blobStorage.OpenReadAsync(storageKey, cancellationToken);
        using var image = await Image.LoadAsync(imageStream, cancellationToken);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(320, 320),
            Mode = ResizeMode.Max
        }));
        await using var output = new MemoryStream();
        await image.SaveAsPngAsync(output, cancellationToken);
        output.Position = 0;
        return await blobStorage.SaveAsync($"thumbnail-{nodeId:N}.png", output, cancellationToken);
    }

    private static bool IsText(string mimeType) =>
        mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
        mimeType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
        mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
        mimeType.Contains("javascript", StringComparison.OrdinalIgnoreCase);
}

