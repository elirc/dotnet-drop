# INC-003 — Bug-bounty report: downloading another account's file

**Severity:** SEV-1 (confidentiality) until scoped
**Branch:** `training/incidents/003-shared-download-exposure`
**Reported:** Thursday 09:20 via the bug-bounty inbox. PR #224 was deployed Wednesday 16:00.

## The report (lightly edited)

> I signed up as `researcher@example.com`. A colleague at a customer (different account, not in any team with me) showed me a NetDropbox screenshot with a file ID in the URL bar. I called:
>
> `GET /api/v1/files/3d9a0c2e-.../download` with **my own** bearer token → **200**, the full PDF.
>
> I tried a second ID from the same screenshot (`81be44f1-...`) → **404 Node not found.**
> Version history (`GET /api/v1/files/3d9a0c2e-.../versions`) → **404** for me.

## Evidence gathered by on-call

```
09:41 INF HTTP GET /api/v1/files/3d9a0c2e-.../download responded 200 in 64 ms  user=c0ffee..(researcher)
09:41 INF HTTP GET /api/v1/files/81be44f1-.../download responded 404 in 12 ms  user=c0ffee..
```

```sql
SELECT id, owner_id, name, is_deleted, scan_status FROM netdropbox.file_nodes
WHERE id IN ('3d9a0c2e-...', '81be44f1-...');
-- 3d9a0c2e | a11ce..(owner@acme) | Q3-board-deck.pdf | f | Clean
-- 81be44f1 | a11ce..(owner@acme) | salaries-2026.xlsx | f | Clean

SELECT file_node_id, shared_by_user_id, shared_with_user_id, role FROM netdropbox.direct_shares
WHERE file_node_id IN ('3d9a0c2e-...', '81be44f1-...');
-- 3d9a0c2e | a11ce.. | b0b..(cfo@acme) | Viewer
```

The researcher has no rows in `direct_shares` and no `share_links` involved.

## The suspect PR (#224, approved, CI green, no new tests)

> **feat(files): let direct-share recipients view versions and download.** "Shared with me" listed files, but opening one returned 404 because download and version history were owner-only. This allows the owner OR a user with a DirectShare on the node.

```diff
--- a/src/NetDropbox.Application/Files/FileService.cs
+++ b/src/NetDropbox.Application/Files/FileService.cs
@@ -95,7 +95,10 @@ public async Task<IReadOnlyCollection<FileVersionDto>> VersionsAsync(...)
-        var node = await db.FileNodes.AsNoTracking().Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId, cancellationToken)
+        // Owners and direct-share recipients can see version history.
+        var node = await db.FileNodes.AsNoTracking().Include(x => x.Versions)
+            .SingleOrDefaultAsync(x => x.Id == nodeId &&
+                (x.OwnerId == userId || db.DirectShares.Any(s => s.FileNodeId == x.Id && s.SharedWithUserId == userId)), cancellationToken)
@@ -113,7 +116,10 @@ public async Task<(Stream Stream, string FileName, string MimeType)> OpenDownloadAsync(...)
-        var node = await db.FileNodes.Include(x => x.Versions).SingleOrDefaultAsync(x => x.Id == nodeId && x.OwnerId == userId && !x.IsDeleted, cancellationToken)
+        // Direct-share recipients (Viewer or Editor) may download as well as the owner.
+        var node = await db.FileNodes.Include(x => x.Versions)
+            .SingleOrDefaultAsync(x => x.Id == nodeId && !x.IsDeleted &&
+                (x.OwnerId == userId || db.DirectShares.Any(s => s.FileNodeId == x.Id)), cancellationToken)
```

## Your tasks

1. Explain the 200 vs 404 difference using only the SQL output above.
2. Scope the blast radius: write the SQL that lists every file that was exposed, and the query that finds who actually used the hole. Which log field do you need, and does this app log it today?
3. Fix it. Then write the test that would have failed on PR #224. Say which layer it runs at (unit, handler with a real DB, or HTTP), and why a domain-only test can't catch it.
4. Structural fix: this app writes authorization inline in each query (`x.OwnerId == userId` is repeated in `FileService`, `ShareService`, `UploadsController`). Propose one mechanism that makes this class of bug harder to write. Give its trade-off.
5. Draft the two-paragraph disclosure note to the affected customer.
