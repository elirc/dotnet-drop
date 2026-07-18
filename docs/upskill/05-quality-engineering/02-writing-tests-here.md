# Writing Tests Here

## Recipes

1. Domain state: add a test that `RestoreVersion` returns status to pending using [FileNode.RestoreVersion](../../../src/NetDropbox.Domain/FileNode.cs#L64-L70).
2. Validation failure: assert chunk checksum length fails through [UploadChunkValidator](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L17-L24).
3. Permission failure: integration-test that user B cannot download user A's node through [FileService.OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122).
4. Scan failure: assert pending/infected statuses fail through [FileService.EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155).
5. Share revocation: create link, revoke via [ShareService.RevokeLinkAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L23-L31), assert public download fails.
6. Worker infected path: feed EICAR text and assert [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L45-L52) marks infected.
7. UI cache invalidation: after upload, assert [refresh](../../../web/src/App.tsx#L62-L65) invalidates files and usage.

Verified test command:

```powershell
& "$HOME\.dotnet\dotnet.exe" test tests/NetDropbox.Tests/NetDropbox.Tests.csproj --no-restore
```

Use filters once test names grow:

```powershell
& "$HOME\.dotnet\dotnet.exe" test tests/NetDropbox.Tests/NetDropbox.Tests.csproj --filter FullyQualifiedName~FileNodeTests
```

The filter command is inferred, not run.

