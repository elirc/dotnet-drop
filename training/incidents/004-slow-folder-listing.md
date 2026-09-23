# INC-004 — Folder listing p95 from 180 ms to 3.7 s

**Severity:** SEV-3 (degraded, some users see timeouts)
**Branch:** `training/incidents/004-slow-folder-listing`
**Alert:** `ApiLatencyP95{route="GET /api/v1/files"} > 1s` fired Friday 11:05. PR #231 was deployed Friday 10:30.

## Metrics

| Metric | Thu | Fri 11:30 |
|---|---|---|
| `GET /api/v1/files` p50 / p95 / p99 | 24 ms / 180 ms / 410 ms | 31 ms / 3.7 s / 11.2 s |
| `GET /api/v1/files/search` p95 | 520 ms | 540 ms |
| Postgres CPU | 18 % | 27 % |
| Postgres active connections (pool max 100) | 11 | 64 |
| API process working set | 390 MB | 1.6 GB, Gen2 GC every ~40 s |
| Statements per second | 1.4 k | 38 k |

`pg_stat_statements`, top by `calls`, last hour:

```
calls     mean_ms  query
9 102 311  0.31    SELECT f.id, f.blob_id, f.content_hash, ... FROM netdropbox.file_versions AS f WHERE f.id = $1 LIMIT $2
  1 544    41.8    SELECT f.id, f.created_at, f.current_version_id, f.deleted_at, f.extracted_text, ... FROM netdropbox.file_nodes AS f WHERE f.owner_id = $1 AND ...
```

## Logs

```
11:12:40 INF HTTP GET /api/v1/files?parentId=9c1e.. responded 200 in 6412 ms  user=4be1.. (items=5 912)
11:12:41 INF HTTP GET /api/v1/files responded 200 in 19 ms                   user=77aa.. (items=14)
11:12:44 INF HTTP GET /api/v1/files?parentId=9c1e.. responded 200 in 5980 ms  user=4be1..
```

(User `4be1..` is the Acme Photo account with a 5,912-file "Camera Roll" folder. The web client refetches the list after every upload, delete and restore.)

## The suspect PR (#231, CI green)

> **fix(files): resolve current version explicitly in folder listing.** Support ticket: some files show "0 B" after "Restore version". Replace the correlated sub-selects in ListAsync with an explicit lookup of the current FileVersion per node.

```diff
--- a/src/NetDropbox.Application/Files/FileService.cs
+++ b/src/NetDropbox.Application/Files/FileService.cs
     public async Task<IReadOnlyCollection<FileNodeDto>> ListAsync(Guid userId, Guid? parentId, bool trash, CancellationToken cancellationToken)
     {
-        var query = db.FileNodes
-            .AsNoTracking()
-            .Where(x => x.OwnerId == userId && x.ParentId == parentId && x.IsDeleted == trash);
-
-        return await query
-            .OrderBy(x => x.Type)
-            .ThenBy(x => x.Name)
-            .Select(x => new FileNodeDto(
-                x.Id, x.ParentId, x.Name, x.Type, x.ScanStatus,
-                x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.Size).FirstOrDefault(),
-                x.Versions.Where(v => v.Id == x.CurrentVersionId).Select(v => v.MimeType).FirstOrDefault(),
-                x.UpdatedAt, x.IsDeleted, x.ThumbnailPath))
-            .ToArrayAsync(cancellationToken);
+        var nodes = await db.FileNodes
+            .AsNoTracking()
+            .Where(x => x.OwnerId == userId && x.ParentId == parentId && x.IsDeleted == trash)
+            .OrderBy(x => x.Type)
+            .ThenBy(x => x.Name)
+            .ToListAsync(cancellationToken);
+
+        // Resolve the current version explicitly. The correlated sub-selects returned
+        // 0 B / null MIME for files whose CurrentVersionId pointed at a restored version.
+        var result = new List<FileNodeDto>(nodes.Count);
+        foreach (var x in nodes)
+        {
+            var current = x.CurrentVersionId is null
+                ? null
+                : await db.FileVersions.AsNoTracking().SingleOrDefaultAsync(v => v.Id == x.CurrentVersionId, cancellationToken);
+            result.Add(new FileNodeDto(x.Id, x.ParentId, x.Name, x.Type, x.ScanStatus, current?.Size ?? 0, current?.MimeType, x.UpdatedAt, x.IsDeleted, x.ThumbnailPath));
+        }
+
+        return result;
     }
```

## Your tasks

1. There are **two** independent regressions in this diff. The metrics table has evidence for both. Name each one and the metric that proves it.
2. Was the PR's premise true? Could the old projection really return 0 B after a version restore? Reason from `FileNode.RestoreVersion` and the old SQL shape. What would you have asked the author for before approving?
3. Write the fix, then describe a test that fails if anyone reintroduces a per-row query. (Hint: think about what you can count.)
4. The listing has no pagination. At what folder size does even the *old* query become a problem, and what would you change in the API contract? Can you do it without breaking the current `web/src/api.ts` client?
