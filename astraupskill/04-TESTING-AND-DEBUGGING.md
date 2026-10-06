# Testing: select the evidence that matches the boundary

The accepted command runs tests/NetDropbox.Tests/NetDropbox.Tests.csproj with the isolated .NET 8 toolchain, disabled build servers, one MSBuild worker, and no restore after the earlier successful dependency setup. Its final record is sol15-netdrop-tests-r4. All twenty-two cases passed with zero skipped. Earlier failed attempts remain in the campaign checks directory. Their failures led to the EF mapping repair and corrections to the new test fixture, rather than being relabeled as successful runs.

Use domain tests for pure decisions. UploadSessionTests covers chunk rounding, strict SHA-256 input, counts that exceed integer capacity, exact final-chunk size, and terminal lifecycle rejection. UserStorageAccountTests covers ordinary quota rejection, arithmetic overflow, and the fact that preflight does not mutate UsedBytes. These cases use concrete values and inspect state after rejection. An assertion that merely observes an exception would miss a method that increments usage before throwing.

Use real filesystem tests for publication behavior. FileSystemChunkStorageTests creates uniquely named temporary directories, exercises invalid duplicates, competing different payloads, repeated accepted bytes, independent assembled streams, and early rejection of oversized input. A filesystem winner is established by the actual accepted file, not by a mock call count. Cleanup runs in finally and targets only the directory created by that fixture. When debugging a failure, inspect the winning bytes and the conflict type before assuming which concurrent task must win.

Use handler tests for orchestration. UploadHandlerTests supplies fake chunk/blob/queue ports but the actual application handlers and EF context model. It reopens the same named InMemory database to inspect usage, file versions, blobs, jobs, and session state after operations. That proves the handler saved the expected graph in this fixture; it does not prove relational uniqueness, SQL rollback, or competing database writers. Avoid seeding the same chunk twice: InMemory does not enforce the SQL unique index, so a duplicated fixture can fail SingleOrDefault before reaching the intended behavior. Preserve the distinction between fixture mistakes and application defects in your debugging notes.

## The 22 test cases, by boundary

20 `[Fact]` + one `[Theory]` with 2 `[InlineData]` rows = 22 cases in 21 methods, matching the
recorded run.

| File | Cases | Boundary | Builds from a clean clone? |
| --- | --- | --- | --- |
| [`Domain/FileNodeTests.cs`](../tests/NetDropbox.Tests/Domain/FileNodeTests.cs) | 3 | pure domain | only if the project builds (see below) |
| [`Domain/UploadSessionTests.cs`](../tests/NetDropbox.Tests/Domain/UploadSessionTests.cs) | 7 (5 facts + theory × 2) | pure domain: rounding (9), invalid hashes (29-37), unrepresentable count (40), exact remainder (55), terminal transitions (67) | same |
| [`Domain/UserStorageAccountTests.cs`](../tests/NetDropbox.Tests/Domain/UserStorageAccountTests.cs) | 3 | pure domain: quota (9), overflow (21), preflight doesn't mutate (33) | same |
| [`Uploads/FileSystemChunkStorageTests.cs`](../tests/NetDropbox.Tests/Uploads/FileSystemChunkStorageTests.cs) | 4 | real temp files | **no:** the class under test isn't in git |
| [`Uploads/UploadHandlerTests.cs`](../tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs) | 5 | real handlers + EF InMemory + fakes (`FakeChunkStorage` line 230, `FakeBlobStorage` line 266) | same |

Because the one test project references Infrastructure, and Infrastructure references the
uncommitted adapters, **none** of the 22 compile on a fresh clone of `main`. CI
([`ci.yml`](../.github/workflows/ci.yml)) would fail at the build step. Note that it builds
only the API project and then runs `dotnet test ... --no-build`, so the test project is
never built in CI at all, which is a second, independent problem. To run the domain tests
before J0, use a worktree at `d612e0b` (6 older domain tests).

**Exercise.** *Goal:* tie one test to the ordering claim it protects. Open
`Complete_late_quota_failure_happens_before_external_write`
([`UploadHandlerTests.cs:34-53`](../tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs#L34-L53)).
**Check:** name the production line that must come *before* the blob write for this test
to pass (`CompleteUpload.cs:46`, `EnsureCanReserve`), and the assertions that would catch a
regression that moved it after the blob write at line 59: `fixture.Chunks.OpenCount.Should().Be(0)`
and `fixture.Blobs.SaveCount.Should().Be(0)` (lines 50-51). Without call-count assertions
on the fakes, a test like this only proves *that* it failed, not *where*. That's the
difference between asserting an outcome and asserting an ordering.

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
