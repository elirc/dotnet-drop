# M3 — Scan state must follow the bytes

**Context.** `ScanStatus` is a column on `FileNode` (`src/NetDropbox.Domain/FileNode.cs:25`), but content lives on `FileVersion` → `BlobObject`. Three code paths break the invariant "only bytes that passed a scan can be downloaded":
- `FileService.CopyAsync` calls `copy.MarkScanClean(...)` unconditionally (`FileService.cs:76`).
- `OpenDownloadAsync` checks the node's status, then serves any `versionId` (`FileService.cs:118-120`).
- `RestoreVersionAsync` sets `PendingScan` but never queues a scan (`FileNode.cs:68`, `FileService.cs:106-112`), so the file is stuck forever.

**Ticket (security).** "Pen-test finding NDB-SEC-07: quarantine bypass via copy and via version download. Fix it so no path can serve bytes that haven't been scanned Clean. Also fix the stuck-after-restore bug support keeps escalating."

**Constraints.** The dedupe design means identical bytes share one `BlobObject` across users. Scanning is per content, not per file. Don't re-scan a blob that's already Clean unless the scanner signature version changed. The public share path (`ShareService.OpenPublicDownloadAsync`) must follow the same rule.

**Definition of done.**
- One authoritative "is this version downloadable" check used by owner download, version download, public share and copy.
- Copying an Infected or Pending file is either refused, or produces a copy with the source's real status. Justify which.
- Restore-version results in a downloadable file without manual intervention.
- Tests for each of the three paths above, plus a public-share test.

**Explain before touching.**
1. Where should scan state live: node, version or blob? Argue from the dedupe design and from what "re-scan with new signatures" requires.
2. If you move the state, what happens to the existing `file_nodes.scan_status` data and to `FileNodeDto.ScanStatus`, which the web client renders (`App.tsx:203`)?
3. Restore-version options: (a) queue a new job, or (b) reuse the version's previous scan result from `scan_results`. Which is correct, and when is each wrong?
4. List every read path that serves bytes. Did you find all four?
5. What's the smallest safe fix you'd ship **today** before the model change? (Hint: S2 does the data move properly.)

<details><summary>Hints</summary>

- The smallest fix today: copy inherits `node.ScanStatus` (it only calls `MarkScanClean` when the source is Clean); download with `versionId` requires a Clean `ScanResult` for that version; restore enqueues a job.
- `scan_results` already records `(FileNodeId, FileVersionId, Status)` (`ScanResult.cs`).
</details>

Answer: `training/_answers/ladder-M3.md`
