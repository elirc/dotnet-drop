# Trace lab: follow one upload and its competing retry

Prepare a trace table with columns for session status, accepted chunk files, metadata rows, UsedBytes, file versions, processing jobs, and external calls. Start with an open ten-byte upload, chunk size four, and enough unused quota. At checkpoint A, there are no accepted files and no chunk rows. The required lengths are four, four, and two. Predict each row before reading the corresponding method.

At checkpoint B, a valid first chunk is written to a unique pending file. Its measured size and digest match the request. A non-overwriting move publishes index zero, then the handler records its metadata. A simultaneous request for different valid bytes cannot replace that path. It inspects the published content and throws a chunk conflict. An identical retry can inspect the same path successfully and leaves the existing metadata row unchanged. Notice that accepted filesystem state may precede database state; this is why retry behavior must be safe even when the metadata lookup initially returns nothing.

At checkpoint C, indexes one and two have also been accepted. Completion verifies the exact index sequence, not merely a plausible count. The account passes a read-only quota check. Assembly opens a unique temporary stream and copies the three accepted files in order. Hashing observes ten bytes. If either observed length or final digest differs, stop the trace here and confirm that no blob or file version has been written by this completion attempt.

At checkpoint D, valid content proceeds to blob lookup or storage. The handler prepares a file version, processing job, quota reservation, and completed session. SaveChanges is the database boundary. At checkpoint E, session chunks are cleaned up and the processing message is enqueued. A repeated completion sees a terminal session and rejects instead of charging quota again. Now insert failures between every pair of checkpoints. Identify which are rejected before side effects, which may leave recoverable temporary or accepted files, and which may leave a saved database job awaiting external work. The current tests cover selected checkpoints; they do not simulate every process crash or concurrent SQL interleaving. Use the table to design that later integration work precisely.

## Source excerpt

From [src/NetDropbox.Application/Uploads/CompleteUpload.cs](../src/NetDropbox.Application/Uploads/CompleteUpload.cs).

```cs
var version = file.AddVersion(blob.Id, assembledContent.Length, session.MimeType, assembledContent.Checksum, request.UserId);
        var scanJob = ProcessingJob.Queue(file.Id, version.Id, ProcessingJobKind.VirusScan);
        db.ProcessingJobs.Add(scanJob);
        user.Reserve(session.TotalSize);
        session.Complete(file.Id);

        await db.SaveChangesAsync(cancellationToken);
        await chunks.DeleteSessionAsync(session.Id, cancellationToken);
        await queue.EnqueueAsync(new ProcessingMessage(file.Id, version.Id, scanJob.Id), cancellationToken);

        return new FileNodeDto(file.Id, file.ParentId, file.Name, file.Type, file.ScanStatus, session.TotalSize, session.MimeType, file.UpdatedAt, file.IsDeleted, file.ThumbnailPath);
    }
}
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
