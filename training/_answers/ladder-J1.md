# ANSWER — J1 upload status 404

**Q1 trace.** `UploadsController.Status` → EF `SingleAsync` throws `InvalidOperationException("Sequence contains no elements")` → propagates through MVC → `ExceptionMiddleware.InvokeAsync` (`ExceptionMiddleware.cs:17-23`) has no case for it → `_ => 500` → title "An unexpected error occurred." and an Error log line. `CorrelationIdMiddleware` wraps it but doesn't change the status.

**Q2.** 404 for a foreign session avoids confirming that the ID exists, so session IDs can't be enumerated. It's the same pattern `FileService.GetOwnedNode` uses (`FileService.cs:143-147`). 403 would leak existence.

**Q3.** Before: the controller holds `IAppDbContext`, and EF entities and `DbSet`s cross into the API layer. After: the controller sends `GetUploadStatusQuery(UserId, SessionId)` and gets `UploadSessionDto` back. Only the DTO crosses. The API project no longer needs `Microsoft.EntityFrameworkCore` usings in the controller.

**Reference handler**
```csharp
public sealed record GetUploadStatusQuery(Guid UserId, Guid SessionId) : IRequest<UploadSessionDto>;
public sealed class GetUploadStatusHandler(IAppDbContext db) : IRequestHandler<GetUploadStatusQuery, UploadSessionDto>
{
    public async Task<UploadSessionDto> Handle(GetUploadStatusQuery q, CancellationToken ct)
    {
        var s = await db.UploadSessions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == q.SessionId && x.OwnerId == q.UserId, ct)
            ?? throw new AppException("Upload session not found.", HttpStatusCode.NotFound);
        var uploaded = await db.UploadChunks.Where(x => x.UploadSessionId == s.Id)
            .OrderBy(x => x.Index).Select(x => x.Index).ToArrayAsync(ct);
        return new UploadSessionDto(s.Id, s.FileName, s.ChunkSize, s.TotalChunks, s.ExpiresAt, uploaded);
    }
}
```
**Expired or completed sessions:** return 200 with the data. Adding `Status` to the DTO is additive JSON (the TS client ignores unknown fields), so it's backward compatible and lets the client restart cleanly instead of failing on chunk PUT with 409. A 410 for expired is defensible, but it changes the client contract.

**Q4.** Test packages: `Microsoft.AspNetCore.Mvc.Testing`, a DB (Testcontainers is referenced but unused; EF InMemory for handler tests), a project reference to `NetDropbox.Api`, `public partial class Program {}`, and a test JWT. Either sign one with the configured key or replace authentication with a test scheme.

**Q5.** `api.ts:74-77` reads `problem.detail ?? problem.title`. `ExceptionMiddleware` puts the `AppException` message in both, so the UI shows "Upload session not found." with no client change.

**Rubric.** Solid means the handler, a 404 and the unit test. Strong means you also noticed the missing `AsNoTracking`, ordered the chunk indexes, added the contract-safe DTO field, and wrote an HTTP test that asserts `application/problem+json`. The current `ExceptionMiddleware` writes plain `WriteAsJsonAsync`, so the content type is `application/json`. Did you catch that?
