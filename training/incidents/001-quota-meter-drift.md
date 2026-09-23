# INC-001 — Usage meter wrong, "Storage quota exceeded" on resume

**Severity:** SEV-3, trending to SEV-2 (paying users blocked from uploading)
**Branch:** `training/incidents/001-quota-meter-drift`
**Started:** within a day of the Monday 10:40 deploy (PR #212, below)

## What support forwarded

> "The sidebar says I've used 4.7 GB of 5 GB. I have maybe 1.3 GB of files, I checked every folder. Today I can't upload anything."

> "My 900 MB video upload stalled when the train went into a tunnel. When I hit upload again it said *Storage quota exceeded*. I deleted two old files to make room and it STILL says exceeded."

## Metrics (Grafana, 7-day window)

| Metric | Before deploy | 48 h after |
|---|---|---|
| `POST /api/v1/uploads` 409 rate | 0.1 % | 6.3 % |
| `POST /api/v1/uploads/{id}/complete` 409 rate | 0.4 % | 0.4 % |
| Σ `users.used_bytes` / Σ bytes in current `file_versions` | 1.02 | 1.61, still climbing |
| Upload sessions reaching `Expired` per day | ~140 | ~150 |

## Logs (Serilog, one affected user, correlation IDs trimmed)

```
10:02:11 INF HTTP POST /api/v1/uploads responded 200 in 38 ms   user=7f3c.. file=trip.mp4 size=943718400
10:09:47 INF HTTP PUT  /api/v1/uploads/5b1e../chunks/61 responded 200 in 912 ms
10:10:02 WRN client disconnected (TaskCanceledException) PUT /api/v1/uploads/5b1e../chunks/62
10:24:30 INF HTTP POST /api/v1/uploads responded 200 in 41 ms   user=7f3c.. file=trip.mp4 size=943718400
10:24:31 INF HTTP GET  /api/v1/uploads/5b1e.. responded 200     uploadedChunks=62
10:31:05 WRN client disconnected PUT /api/v1/uploads/5b1e../chunks/140
10:52:18 INF HTTP POST /api/v1/uploads responded 409 in 22 ms   "Storage quota exceeded."
```

## Database spot-check (read replica)

```sql
SELECT u.email, u.used_bytes, u.quota_bytes,
       (SELECT coalesce(sum(v.size),0) FROM netdropbox.file_nodes n
          JOIN netdropbox.file_versions v ON v.id = n.current_version_id
         WHERE n.owner_id = u.id AND NOT n.is_deleted) AS current_bytes
FROM netdropbox.users u WHERE u.id = '7f3c...';
-- used_bytes 5 033 164 800 | quota 5 368 709 120 | current_bytes 1 391 460 352
```

## The suspect PR (#212, approved by 1 reviewer, CI green)

> **feat(uploads): reserve quota when an upload session starts.** Previously StartUploadHandler only *checked* UsedBytes + TotalSize and the reservation happened in CompleteUploadHandler. Four parallel uploads could each pass the check and oversubscribe. Tested: uploaded 3 files in parallel, the usage meter updates immediately.

```diff
--- a/src/NetDropbox.Application/Uploads/StartUpload.cs
+++ b/src/NetDropbox.Application/Uploads/StartUpload.cs
@@ -38,6 +38,10 @@
         if (user.UsedBytes + request.TotalSize > user.QuotaBytes)
             throw new AppException("Storage quota exceeded.", System.Net.HttpStatusCode.Conflict);

+        // Reserve at session start so several parallel uploads cannot jointly
+        // oversubscribe the quota (previously only checked here, reserved at complete).
+        user.Reserve(request.TotalSize);
+
         if (request.ParentId is not null)
--- a/src/NetDropbox.Application/Uploads/CompleteUpload.cs
+++ b/src/NetDropbox.Application/Uploads/CompleteUpload.cs
@@ -76,9 +76,7 @@
         session.Complete(file.Id);

-        var user = await db.Users.SingleAsync(x => x.Id == request.UserId, cancellationToken);
-        user.Reserve(session.TotalSize);
-
+        // Quota is now reserved in StartUploadHandler.
         await db.SaveChangesAsync(cancellationToken);
```

## Your tasks

1. List at least three hypotheses that fit **all** the evidence, including the second support quote (deleting files didn't help). Rank them.
2. Name every code path that changes `users.used_bytes`, and every path that *should* change it. Cite files.
3. Write a failing test that reproduces the symptom. (A handler test needs a DbContext. Say what you'd use and why the current test project can't do it yet.)
4. Propose the fix **and** the repair of the corrupted `used_bytes` data. Is the repair safe to run while users are uploading?
5. Write the postmortem. Include the question the reviewer should have asked.
