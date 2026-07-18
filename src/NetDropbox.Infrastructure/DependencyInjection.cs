using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetDropbox.Application.Abstractions;
using NetDropbox.Application.Files;
using NetDropbox.Application.Shares;
using NetDropbox.Infrastructure.Persistence;
using NetDropbox.Infrastructure.Processing;
using NetDropbox.Infrastructure.Services;
using NetDropbox.Infrastructure.Storage;

namespace NetDropbox.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=netdropbox;Username=netdropbox;Password=netdropbox";

        services.AddDbContext<NetDropboxDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<NetDropboxDbContext>());
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IProcessingQueue, ChannelProcessingQueue>();
        services.AddScoped<FileService>();
        services.AddScoped<ShareService>();

        var chunkRoot = configuration["Chunks:Root"] ?? Path.Combine(AppContext.BaseDirectory, "chunks");
        services.AddSingleton<IChunkStorage>(_ => new FileSystemChunkStorage(chunkRoot));

        var provider = configuration["Storage:Provider"] ?? "Local";
        if (provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = configuration["Storage:S3:ServiceUrl"],
                ForcePathStyle = true
            };
            services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
                configuration["Storage:S3:AccessKey"],
                configuration["Storage:S3:SecretKey"],
                s3Config));
            services.AddSingleton<IBlobStorage>(sp => new S3CompatibleBlobStorage(
                sp.GetRequiredService<IAmazonS3>(),
                configuration["Storage:S3:Bucket"] ?? "netdropbox"));
        }
        else
        {
            var root = configuration["Storage:LocalRoot"] ?? Path.Combine(AppContext.BaseDirectory, "storage");
            services.AddSingleton<IBlobStorage>(_ => new LocalBlobStorage(root));
        }

        services.AddHostedService<ProcessingWorker>();
        services.AddHostedService<JanitorWorker>();
        return services;
    }
}
