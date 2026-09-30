# Worked change: validate before publication and completion

Begin with the original behavior recorded in this project's snapshots. The chunk storage path wrote directly to the accepted filename before the handler compared the checksum. A malformed retry could therefore replace good bytes while leaving the old metadata intact. The completion path computed a hash but trusted the declared length, and quota reservation happened after external blob work. Those orderings made failures observable as state damage rather than clean rejection.

The revised storage method creates a unique pending filename. It reads at most the remaining expected bytes plus one, allowing it to detect an oversized stream without consuming an arbitrary body. Accepted bytes are hashed and written to the pending file. A matching size and checksum allow a non-overwriting move to the final chunk path. If another request already published there, the method hashes that accepted file and either recognizes an identical retry or rejects conflicting content. Finally removes the pending file when it still exists. Each assembled stream also has its own filename and delete-on-close lifetime, so overlapping readers do not share one mutable assembly path.

At completion, the handler checks index coverage and quota before assembling. The hash helper returns HashedContent rather than only a string, exposing Length for the new comparison. Wrong length or wrong digest stops before blob storage or graph changes. A valid upload proceeds through blob reference accounting, file/version creation, processing-job creation, quota reservation, and session completion. The existing SaveChanges boundary remains visible rather than being hidden in storage helpers.

The new fresh-context handler tests initially exposed a separate production mapping problem: the Versions property and an explicitly named backing-field relationship both attempted to use the same field. Mapping HasMany through Versions and retaining field access resolves that model validation conflict. This is part of the delivered repair, not a test-only bypass. Compare each changed file with its saved original, then explain why validation, temporary publication, graph mutation, and post-commit work occur in their current order. A successful review should account for the failure path at every step, including the steps outside database ownership.

## Source excerpt

From [src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs](../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs).

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
