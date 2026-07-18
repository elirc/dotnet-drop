# Review Katas

## Kata 1: "Add Preview Download For Shared Links"

**Author intent:** public links should preview files.

**Fake diff summary:** calls `OpenPublicDownloadAsync(token, password)` from a new preview endpoint but skips rate limiting.

**Resembles:** [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17).

Expected findings: Blocking - public endpoint must keep rate limiting and must not leak password in URL if preview uses repeated requests.

Good comment:

> Blocking: this creates a new anonymous token path without the public rate-limit policy used by the existing download endpoint. Please apply the same policy or explain why preview has a lower abuse risk.

## Kata 2: "Speed Up Search"

Fake diff summary: calls `.AsEnumerable()` before filtering.

Resembles: [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134).

Expected findings: Blocking - moves filtering from DB to app memory.

## Kata 3: "Simplify Upload Completion"

Fake diff summary: removes final SHA verification because chunks already have checksums.

Resembles: [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L48).

Expected findings: Blocking - final hash protects order and full assembly.

## Kata 4: "Use DbContext In Worker Constructor"

Fake diff summary: injects `NetDropboxDbContext` directly into singleton hosted service.

Resembles: [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L30).

Expected findings: Blocking - captive scoped dependency.

