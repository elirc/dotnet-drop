# ANSWER — INC-002 scan backlog

## Root cause
`ProcessingWorker.ExecuteAsync` (`src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs:18-24`) is a **single serial consumer**: `await foreach (...) await ProcessAsync(...)`. PR #219 put `await Task.Delay(backoff)` **inside** `ProcessAsync`, so the backoff blocks the only consumer instead of delaying just the failing message. Head-of-line blocking follows.

Trigger: Acme's iPhone camera roll contains HEIC files. The browser reports them as `image/heic`, which passes `version.MimeType.StartsWith("image/")` (`ProcessingWorker.cs:57`). ImageSharp has no HEIC decoder, so `GenerateThumbnailAsync` throws every time. Each such file is a **deterministic poison message**: attempt 1 fails → sleep 5 s → attempt 2 fails → sleep 25 s → attempt 3 dead-letters. That's about 30 s of consumer time per HEIC file. 2,000 files ≈ 16 hours of blocked queue. The ~2 completions per minute in the query match that. Everything else waits behind it.

Before the PR, the same exception burned three attempts in milliseconds. It dead-lettered the HEIC files (a separate bug: a thumbnail failure should not fail the *virus scan*) but didn't block anyone.

## Answers to the brief's questions
- **CPU down, latency up** means the system is *waiting*, not working: sleeps, lock waits, pool starvation, or a serialized consumer. Other candidates in this repo: a slow `S3CompatibleBlobStorage.OpenReadAsync` holding the single consumer; `JanitorWorker` holding locks during a large `RemoveRange`.
- **Failed → Clean flipping:** the catch block calls `node.MarkScanFailed()` on every *retryable* failure (`ProcessingWorker.cs:71`). Users see "scan failed" (download returns 409, `FileService.EnsureDownloadable`) for a file that will pass on retry. Transient state leaks into user-visible state.
- **Will the 1,842 Queued rows drain?** Only if their messages are still in the in-memory `Channel` (`ChannelProcessingQueue.cs:8`). The worker never reads `processing_jobs`. Rows are write-only bookkeeping. If you restart the pod to "unstick" it, every queued message is lost and those files stay PendingScan forever. **Don't restart as a mitigation** until a re-enqueue path exists.

## Fix
1. Never sleep in the consumer loop. Either re-enqueue with a *not-before* time (a delayed queue or a DB column `next_attempt_at`, polled), or run N consumers with a bounded concurrency (`Parallel.ForEachAsync` with `MaxDegreeOfParallelism`, a scope per message).
2. Separate the steps: virus scan decides Clean/Infected; thumbnail and text extraction are best-effort. A thumbnail exception must not fail the scan job. Only decode formats ImageSharp supports (allow-list), and treat `UnknownImageFormatException` as non-retryable.
3. Classify errors: deterministic ones go straight to dead-letter, transient ones retry. Don't mark the node Failed until the job is dead-lettered.
4. Recovery: a startup sweep re-enqueues `processing_jobs` in `Queued` or `Failed` (this is the DB-polling version of the outbox; see ladder S1).

## Regression test
Unit test the worker with a fake `IProcessingQueue` and a fake blob store: enqueue [poison, good]. Assert that the good message completes within 1 s, while the poison message is still in backoff. Use an injectable delay/clock so the test doesn't really sleep. The current code has no seam: `Task.Delay` and `DateTimeOffset.UtcNow` are called directly, which is itself a review finding.

## Postmortem notes
- **Detection** worked (the scan-latency SLO alert). **Diagnosis** was slowed by the new log levels: failures became Warnings, so the ERR-rate dashboard stayed flat.
- **Why review missed it:** the diff was local to the catch block. The loop that awaits `ProcessAsync` was 45 lines above and not in the hunk. Reviewer question: *"Who else is waiting while this line sleeps?"*
- **Actions:** stage separation, error classification, bounded parallelism, a queue-depth metric and an oldest-queued-age metric, a DB-backed re-enqueue sweep.
