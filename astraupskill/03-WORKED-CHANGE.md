# Worked change: validate before publication and completion

Begin with the original behavior recorded in this project's snapshots. The chunk storage path wrote directly to the accepted filename before the handler compared the checksum. A malformed retry could therefore replace good bytes while leaving the old metadata intact. The completion path computed a hash but trusted the declared length, and quota reservation happened after external blob work. Those orderings made failures observable as state damage rather than clean rejection.

The revised storage method creates a unique pending filename. It reads at most the remaining expected bytes plus one, allowing it to detect an oversized stream without consuming an arbitrary body. Accepted bytes are hashed and written to the pending file. A matching size and checksum allow a non-overwriting move to the final chunk path. If another request already published there, the method hashes that accepted file and either recognizes an identical retry or rejects conflicting content. Finally removes the pending file when it still exists. Each assembled stream also has its own filename and delete-on-close lifetime, so overlapping readers do not share one mutable assembly path.

At completion, the handler checks index coverage and quota before assembling. The hash helper returns HashedContent rather than only a string, exposing Length for the new comparison. Wrong length or wrong digest stops before blob storage or graph changes. A valid upload proceeds through blob reference accounting, file/version creation, processing-job creation, quota reservation, and session completion. The existing SaveChanges boundary remains visible rather than being hidden in storage helpers.

The new fresh-context handler tests initially exposed a separate production mapping problem: the Versions property and an explicitly named backing-field relationship both attempted to use the same field. Mapping HasMany through Versions and retaining field access resolves that model validation conflict. This is part of the delivered repair, not a test-only bypass. Compare each changed file with its saved original, then explain why validation, temporary publication, graph mutation, and post-commit work occur in their current order. A successful review should account for the failure path at every step, including the steps outside database ownership.

## Before → after, by file

Each changed file's original is saved in [`snapshots/`](snapshots/) and is byte-identical to
commit `d612e0b`, so `git diff d612e0b 9d28a29 -- src` shows the same change.

| File | Before (`d612e0b`) | After (`main`) |
| --- | --- | --- |
| [`UploadSession.cs`](../src/NetDropbox.Domain/UploadSession.cs) | `Math.Ceiling(totalSize / (double)chunkSize)` cast to `int`; any non-blank hash accepted; `Complete`/`Expire` callable in any state | integer ceiling with an `int.MaxValue` guard (lines 42-44); `IsSha256` (line 40); `EnsureOpen` on transitions; new `ExpectedChunkSize` (66-73) |
| [`UserStorageAccount.cs`](../src/NetDropbox.Domain/UserStorageAccount.cs) | `UsedBytes + bytes > QuotaBytes`, which can overflow | `EnsureCanReserve` compares against remaining capacity by subtraction and mutates nothing (42-47); `Reserve` calls it (35-40) |
| [`UploadChunk.cs`](../src/NetDropbox.Application/Uploads/UploadChunk.cs) | storage wrote the final path first, then the handler compared checksums | changed-duplicate check before storage (40-47); expected size passed to storage (57); conflict → 409 (60-63) |
| [`CompleteUpload.cs`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs) | hash checked, declared length trusted; quota reserved after the blob write | preflight (46) → assemble → length (50) → digest (52) → blob → graph → reserve → one save (85) |
| [`Hashing.cs`](../src/NetDropbox.Application/Common/Hashing.cs) | returned a hash string | returns `HashedContent(Checksum, Length)` (line 24) |
| [`IChunkStorage.cs`](../src/NetDropbox.Application/Abstractions/IChunkStorage.cs) | no expected size, no conflict type | `StoreAsync(..., expectedSize, ...)`, `StoredChunk.Committed`, `ChunkWriteConflictException` |
| [`NetDropboxDbContext.cs`](../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs) | `HasMany<FileVersion>("_versions")` plus `Navigation(x => x.Versions)` on the same field, so the model throws | `HasMany(x => x.Versions)` + field access (44-48) |
| `Storage/FileSystemChunkStorage.cs` | in `snapshots/` only | **not committed**, so you can't diff it |

**Exercise.** *Goal:* verify one row of the table yourself instead of trusting it.
Run `git diff d612e0b 9d28a29 -- src/NetDropbox.Domain/UserStorageAccount.cs` and find a
pair of `long` values for which the *old* check passes when it should fail. **Check:** with
`QuotaBytes = 10`, `UsedBytes = 8`, `bytes = long.MaxValue`, the old `8 + long.MaxValue`
wraps negative, so `> 10` is false and the reservation is *accepted*. The new
`bytes > QuotaBytes - UsedBytes` (`long.MaxValue > 2`) rejects it.
`Reserve_rejects_overflow_without_changing_usage`
([`UserStorageAccountTests.cs:20-31`](../tests/NetDropbox.Tests/Domain/UserStorageAccountTests.cs#L20-L31))
pins the new behavior.

## Source excerpt

From `src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs`, as it existed in the author's working copy. **This file is not in git** (see [README](README.md)), so the excerpt below can't be checked against the repository. The committed original is in [`snapshots/`](snapshots/src__NetDropbox.Infrastructure__Storage__FileSystemChunkStorage.cs.original.txt).

```cs
var accepted = total == expectedSize &&
                string.Equals(checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);
            if (accepted)
            {
                try
                {
                    File.Move(tempPath, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    var published = await InspectAsync(path, cancellationToken);
                    if (published.Size != expectedSize ||
                        !string.Equals(published.Checksum, expectedChecksum, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ChunkWriteConflictException(
                            "A different chunk was already published for this session index.");
                    }
                }
            }

            return new StoredChunk(checksum, total, accepted);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
