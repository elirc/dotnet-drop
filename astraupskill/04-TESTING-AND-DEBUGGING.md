# Testing: select the evidence that matches the boundary

The accepted command runs tests/NetDropbox.Tests/NetDropbox.Tests.csproj with the isolated .NET 8 toolchain, disabled build servers, one MSBuild worker, and no restore after the earlier successful dependency setup. Its final record is sol15-netdrop-tests-r4. All twenty-two cases passed with zero skipped. Earlier failed attempts remain in the campaign checks directory. Their failures led to the EF mapping repair and corrections to the new test fixture, rather than being relabeled as successful runs.

Use domain tests for pure decisions. UploadSessionTests covers chunk rounding, strict SHA-256 input, counts that exceed integer capacity, exact final-chunk size, and terminal lifecycle rejection. UserStorageAccountTests covers ordinary quota rejection, arithmetic overflow, and the fact that preflight does not mutate UsedBytes. These cases use concrete values and inspect state after rejection. An assertion that merely observes an exception would miss a method that increments usage before throwing.

Use real filesystem tests for publication behavior. FileSystemChunkStorageTests creates uniquely named temporary directories, exercises invalid duplicates, competing different payloads, repeated accepted bytes, independent assembled streams, and early rejection of oversized input. A filesystem winner is established by the actual accepted file, not by a mock call count. Cleanup runs in finally and targets only the directory created by that fixture. When debugging a failure, inspect the winning bytes and the conflict type before assuming which concurrent task must win.

Use handler tests for orchestration. UploadHandlerTests supplies fake chunk/blob/queue ports but the actual application handlers and EF context model. It reopens the same named InMemory database to inspect usage, file versions, blobs, jobs, and session state after operations. That proves the handler saved the expected graph in this fixture; it does not prove relational uniqueness, SQL rollback, or competing database writers. Avoid seeding the same chunk twice: InMemory does not enforce the SQL unique index, so a duplicated fixture can fail SingleOrDefault before reaching the intended behavior. Preserve the distinction between fixture mistakes and application defects in your debugging notes.

## Source excerpt

From [tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs](../tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs).

```cs
public async Task Complete_wrong_assembled_length_preserves_persistent_state()
    {
        var bytes = "too-long"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: 4, content: bytes, quota: 100);

        await using (var db = fixture.CreateContext())
        {
            var handler = new CompleteUploadHandler(db, fixture.Chunks, fixture.Blobs, fixture.Queue, fixture.Clock);
            var action = () => handler.Handle(
                new CompleteUploadCommand(fixture.UserId, fixture.SessionId),
                CancellationToken.None);

            await action.Should().ThrowAsync<AppException>().WithMessage("*size*");
        }

        fixture.Blobs.SaveCount.Should().Be(0);
        await fixture.AssertUncommittedAsync();
    }

    [Fact]
    public async Task Complete_late_quota_failure_happens_before_external_write()
    {
        var bytes = "data"u8.ToArray();
        var fixture = await Fixture.CreateAsync(declaredSize: bytes.Length, content: bytes, quota: bytes.Length + 1, alreadyUsed: 2);
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
