# Practice: predict rejection, retry, and persistence

Complete these exercises without changing the working application first. For every answer, identify the owner of the rule, the observable result, and the state that must remain unchanged. Use the source excerpts and the tests as evidence. Do not treat a familiar exception message as proof that the operation was safe; explain which side effects could have occurred before it was thrown.

Exercise one: declare a ten-byte file with a chunk size of four. Write the required index set, the expected length at each index, and the result of requesting index three. Then repeat the calculation for an empty file. Inspect UploadSession rather than assuming that zero bytes implies zero chunks. Finally consider a positive size requiring more than int.MaxValue chunks. Explain why this request is rejected before allocating a collection of that size.

Exercise two: two clients send different valid thirteen-byte bodies for the same session index. Both calculate a correct SHA-256 for their own body, and neither sees database metadata initially. Predict which outcomes are permitted after both storage calls finish. Your answer must explain what remains on disk and why the database loser cannot silently replace the winner's bytes. Next retry using exactly the bytes found in the accepted file. State whether that storage call should succeed.

Exercise three: a user has a quota of five bytes and already uses two. They complete a four-byte upload. List the earliest rejecting operation and whether assembly, external blob storage, file creation, or usage mutation should occur. Compare that with an eight-byte assembled stream for a declaration of four bytes under a large quota. Both reject, but at different boundaries.

**Checks** (confirm against tests after you write your answers): exercise one → `Expected_chunk_size_uses_full_chunks_and_exact_final_remainder` and `Upload_session_rejects_chunk_count_that_cannot_be_represented` ([`UploadSessionTests.cs:54-65`](../tests/NetDropbox.Tests/Domain/UploadSessionTests.cs#L54-L65), [39-52](../tests/NetDropbox.Tests/Domain/UploadSessionTests.cs#L39-L52)); exercise two → `Concurrent_different_chunks_publish_one_immutable_winner` ([`FileSystemChunkStorageTests.cs:54-90`](../tests/NetDropbox.Tests/Uploads/FileSystemChunkStorageTests.cs#L54-L90)), whose class under test isn't in git, so you're checking against the test's expectations, not the implementation; exercise three → `Complete_late_quota_failure_happens_before_external_write` and `Complete_wrong_assembled_length_preserves_persistent_state` ([`UploadHandlerTests.cs:14-53`](../tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs#L14-L53)); exercise four has no test. The gap between `SaveChangesAsync` and `EnqueueAsync` is [`CompleteUpload.cs:85-87`](../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L85-L87), and `ChannelProcessingQueue` is in-memory, so nothing replays a lost message.

Exercise four: inspect the final completion sequence and identify the window between SaveChanges and EnqueueAsync. Propose a separate recovery design that preserves the database processing job without deleting shared blobs. Explain what additional relational or queue integration evidence your design would need. Finally review the InMemory fixture and name two production guarantees it cannot establish. Submit a short state table and a proposed regression for each exercise before reading the answer chapter. Passing the software suite is evidence about the code, not evidence that a learner has demonstrated these concepts.

## Source excerpt

From [src/NetDropbox.Domain/UploadSession.cs](../src/NetDropbox.Domain/UploadSession.cs).

```cs
public long ExpectedChunkSize(int index)
    {
        if (index < 0 || index >= TotalChunks) throw new DomainException("Chunk index is outside the session range.");
        if (TotalSize == 0) return 0;

        var offset = (long)index * ChunkSize;
        return Math.Min(ChunkSize, TotalSize - offset);
    }

    private static bool IsSha256(string? value) =>
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
