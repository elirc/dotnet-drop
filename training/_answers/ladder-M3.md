# ANSWER — M3 scan state follows the bytes

**Q1.** Scan outcome is a property of **content**, so a blob (content hash) scanned with signature set S is Clean or Infected for everyone. Dedupe (`CompleteUpload.cs:51-61`) means one blob serves many versions and users, so scanning per blob saves work, and an Infected verdict applies to all references. The per-version view is useful for audit ("which scan cleared the bytes this version points at"). **Reference model:** `blob_scans(blob_id, scanner, signature_version, status, scanned_at)`. A version is downloadable iff its blob's latest scan with an acceptable signature version is Clean. Node-level `ScanStatus` becomes a *derived* display value (the current version's status).

**Q2.** Keep `FileNodeDto.ScanStatus`, computed from the current version's blob, so the client (`App.tsx:203`, `scanLabel`) is unchanged. The existing `file_nodes.scan_status` data backfills `blob_scans` for current versions only. Older versions have no verdict and must be treated as `PendingScan` (and scanned when requested). S2 covers the zero-downtime move.

**Q3.** (a) Queue a new job: always correct, but slower. (b) Reuse the prior `ScanResult` for that version: fast, but wrong if signatures changed since (the file could be Infected under today's definitions). Reference: reuse if a Clean result exists with the current signature version, otherwise queue. This repo's `ScanResult.Scanner` is a constant (`"simulated-eicar"`) and has no signature version, so add one.

**Q4. The four byte-serving paths:** `FileService.OpenDownloadAsync` (current version), the same method with `versionId`, `ShareService.OpenPublicDownloadAsync`, and indirectly `FileService.CopyAsync` (it creates a new downloadable reference). Also note that `ProcessingWorker` stores thumbnails from possibly-infected images *before* the verdict. The thumbnail pipeline should run only after Clean.

**Q5. Smallest safe fix today (ship before the model change):**
```csharp
// CopyAsync
if (node.ScanStatus == FileScanStatus.Clean) copy.MarkScanClean(node.ExtractedText, node.ThumbnailPath);
else if (node.ScanStatus == FileScanStatus.Infected) throw new AppException("File is quarantined.", HttpStatusCode.Forbidden);
else { /* PendingScan/Failed: enqueue a scan job for the copy's version */ }
// OpenDownloadAsync with versionId
if (versionId is not null && versionId != node.CurrentVersionId &&
    !await db.ScanResults.AnyAsync(r => r.FileVersionId == versionId && r.Status == FileScanStatus.Clean, ct))
    throw new AppException("Version has not passed a scan.", HttpStatusCode.Conflict);
// RestoreVersionAsync: after node.RestoreVersion(...) add a ProcessingJob and enqueue (or reuse a Clean ScanResult)
```
Refusing to copy Infected files is simpler to reason about than "copy but stay Infected", and there's no user value in duplicating quarantined content.

**Tests.** (1) Infected source → copy returns 403 or the copy is not Clean. (2) v1 Infected, v2 Clean → `download?versionId=v1` returns 409/403. (3) Restore v1 (Clean) → the job exists, or the status is Clean. (4) Public share of a node whose current version isn't Clean → 409. They all need a DB (handler level).
