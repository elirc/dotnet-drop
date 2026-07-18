using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetDropbox.Application.Abstractions;
using NetDropbox.Domain;

namespace NetDropbox.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NetDropboxDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (await db.Users.AnyAsync(cancellationToken)) return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var owner = UserStorageAccount.Create("owner@netdropbox.local", "Owner", 5L * 1024 * 1024 * 1024);
        owner.SetPasswordHash(hasher.Hash("Passw0rd!"));
        var teammate = UserStorageAccount.Create("teammate@netdropbox.local", "Teammate", 2L * 1024 * 1024 * 1024);
        teammate.SetPasswordHash(hasher.Hash("Passw0rd!"));
        db.Users.AddRange(owner, teammate);

        db.FileNodes.Add(FileNode.CreateFolder(owner.Id, null, "Projects"));
        await db.SaveChangesAsync(cancellationToken);
    }
}
