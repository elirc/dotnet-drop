# Netdrop: preserve upload integrity across retries

An upload is a promise about bytes. The caller declares a total length, a final SHA-256 digest, and a chunk size. Netdrop must keep that promise through retries, filesystem writes, quota checks, and database persistence. This course follows the existing upload implementation instead of presenting a separate sample application. Start with UploadSession, follow UploadChunkHandler into FileSystemChunkStorage, and finish with CompleteUploadHandler and UserStorageAccount. The application and infrastructure projects retain their existing roles.

The improvement closes several concrete gaps. Chunk counts now use integer arithmetic that does not overflow while rounding upward. Final hashes must contain exactly sixty-four hexadecimal characters. Every chunk has an expected length, including the smaller final remainder. Incoming bytes go to a temporary file; only validated bytes become an accepted chunk. Publication cannot overwrite a different accepted chunk, including when two first requests overlap. A repeated identical body remains acceptable. Completion checks both the assembled digest and actual length before storing a blob or changing the database graph.

Quota validation is deliberately split into a read-only preflight and the eventual reservation. The preflight rejects known insufficient capacity before external blob storage is touched. The reservation belongs with the file version, processing job, and completed session at SaveChanges. This distinction avoids modifying tracked usage merely to discover a later validation failure. A separate EF relationship mapping repair allows the real application context model to initialize correctly.

Work through the map, concepts, worked change, tests, exercises, and trace in that order. Predict persisted state after every failure, not just the returned exception. The accepted suite contains twenty-two passing tests. Filesystem tests exercise real temporary files; handler tests use fresh EF InMemory contexts and fake external storage and queue ports. Those tests do not establish SQL isolation or a distributed transaction between database, blob storage, and processing queue. Keep that distinction beside every reliability claim you make about the implementation.

## Source excerpt

From [src/NetDropbox.Application/Uploads/CompleteUpload.cs](../src/NetDropbox.Application/Uploads/CompleteUpload.cs).

```cs
public sealed class CompleteUploadHandler(
    IAppDbContext db,
    IChunkStorage chunks,
    IBlobStorage blobs,
    IProcessingQueue queue,
    IClock clock) : IRequestHandler<CompleteUploadCommand, FileNodeDto>
{
    public async Task<FileNodeDto> Handle(CompleteUploadCommand request, CancellationToken cancellationToken)
    {
        var session = await db.UploadSessions.SingleOrDefaultAsync(x => x.Id == request.SessionId && x.OwnerId == request.UserId, cancellationToken)
            ?? throw new AppException("Upload session not found.", System.Net.HttpStatusCode.NotFound);

        if (session.Status != UploadSessionStatus.Open || session.IsExpired(clock.UtcNow))
            throw new AppException("Upload session is no longer open.", System.Net.HttpStatusCode.Conflict);

        var chunkIndexes = await db.UploadChunks
            .Where(x => x.UploadSessionId == session.Id)
            .OrderBy(x => x.Index)
            .Select(x => x.Index)
            .ToArrayAsync(cancellationToken);

        if (chunkIndexes.Length != session.TotalChunks || !chunkIndexes.SequenceEqual(Enumerable.Range(0, session.TotalChunks)))
            throw new AppException("Upload is missing chunks.", System.Net.HttpStatusCode.Conflict);

        var user = await db.Users.SingleAsync(x => x.Id == request.UserId, cancellationToken);
        user.EnsureCanReserve(session.TotalSize);

        await using var assembled = await chunks.OpenAssembledAsync(session, chunkIndexes, cancellationToken);
        var assembledContent = await Hashing.Sha256WithLengthAsync(assembled, cancellationToken);
        if (assembledContent.Length != session.TotalSize)
            throw new AppException("Final file size does not match the upload session.", System.Net.HttpStatusCode.BadRequest);
        if (!string.Equals(assembledContent.Checksum, session.FinalContentHash, StringComparison.OrdinalIgnoreCase))
            throw new AppException("Final file checksum mismatch.", System.Net.HttpStatusCode.BadRequest);

        assembled.Position = 0;
        var blob = await db.Blobs.SingleOrDefaultAsync(x => x.ContentHash == assembledContent.Checksum, cancellationToken);
        if (blob is null)
        {
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)


Continue with the [extended engineering course](extended-course/README.md) for the new source-grounded chapters, separate reviews, capstones, and disposable labs. The extended index reports its continuing scope.
