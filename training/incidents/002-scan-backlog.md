# INC-002 — Files stuck "Pending" for tens of minutes

**Severity:** SEV-2 (no new file can be downloaded or previewed until its scan finishes)
**Branch:** `training/incidents/002-scan-backlog`
**Alert:** `ScanLatencyP95 > 5m for 10m` fired Wednesday 15:12. The deploy of PR #219 went out Tuesday 17:30.

## What users see

- New uploads stay on a grey **Pending** pill. Opening one shows *"Preview is blocked until scan passes."*
- `GET /api/v1/files/{id}/download` → `409 File is still being scanned.` rate went from 0.3 % to 41 %.
- A few files flip to **Failed**, then back to **Clean** minutes later.

## Metrics

| Metric | Tue 12:00 | Wed 15:15 |
|---|---|---|
| p95 upload-complete → scan-clean | 2.1 s | 38 min |
| API CPU (single instance) | 22 % | 4 % |
| API memory | 410 MB | 430 MB |
| Postgres CPU | 9 % | 5 % |
| `processing_jobs` by status | Queued 3 · Completed 51 k | Queued 1 842 · Running 1 · Failed 9 · DeadLettered 214 · Completed 52 k |

```sql
SELECT date_trunc('minute', updated_at) m, count(*) FROM netdropbox.processing_jobs
WHERE status = 'Completed' AND updated_at > now() - interval '30 min' GROUP BY 1 ORDER BY 1 DESC LIMIT 5;
-- 15:14 | 2      15:13 | 1      15:12 | 2      15:11 | 3      15:10 | 1
```

## Logs (worker category, sampled)

```
14:58:03 WRN Processing job 0c41.. failed (attempt 1), retrying in 00:00:05
14:58:08 WRN Processing job 0c41.. failed (attempt 2), retrying in 00:00:25
14:58:33 ERR Processing job 0c41.. dead-lettered after 3 attempts
         SixLabors.ImageSharp.UnknownImageFormatException: Image cannot be loaded. Available decoders: PNG, JPEG, GIF, BMP, ...
14:58:34 WRN Processing job 9e02.. failed (attempt 1), retrying in 00:00:05
...
```

Context from the customer-success channel: *"Acme Photo imported their iPhone camera roll into NetDropbox yesterday evening, around 2,000 files."*

## The suspect PR (#219, CI green)

> **fix(worker): back off before re-enqueueing failed processing jobs.** A failing job was re-enqueued immediately and hot-looped through its three attempts in milliseconds, so transient blob-store hiccups dead-lettered files for no reason. Add exponential backoff (5 s, 25 s).

```diff
--- a/src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs
+++ b/src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs
@@ -66,12 +66,20 @@
         catch (Exception ex)
         {
-            logger.LogError(ex, "Processing job {JobId} failed", message.JobId);
             job.MarkFailed(ex.Message);
             node.MarkScanFailed();
             await db.SaveChangesAsync(cancellationToken);
-            if (job.Status != ProcessingJobStatus.DeadLettered)
-                await queue.EnqueueAsync(message, cancellationToken);
+            if (job.Status == ProcessingJobStatus.DeadLettered)
+            {
+                logger.LogError(ex, "Processing job {JobId} dead-lettered after {Attempts} attempts", message.JobId, job.Attempts);
+                return;
+            }
+
+            // Exponential backoff instead of hot-looping a failing job: 5s, 25s.
+            var backoff = TimeSpan.FromSeconds(Math.Pow(5, job.Attempts));
+            logger.LogWarning("Processing job {JobId} failed (attempt {Attempts}), retrying in {Backoff}", message.JobId, job.Attempts, backoff);
+            await Task.Delay(backoff, cancellationToken);
+            await queue.EnqueueAsync(message, cancellationToken);
         }
```

## Your tasks

1. CPU is *down* while latency is *up*. What kind of problem produces that signature? Name two other bugs in this codebase that would also produce it.
2. Estimate the throughput of the worker right now from the numbers above. Check your estimate against the "Completed per minute" query.
3. The ImageSharp exception was already happening before the PR. Why was it harmless on Tuesday and harmful on Wednesday?
4. Fix it without dropping the backoff. Then list what else the files "flipping Failed → Clean" tells you about the retry design.
5. After the fix ships, 1,842 jobs are still Queued in the table. Will they drain? Look carefully at where the worker gets its work from.
