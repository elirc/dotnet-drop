# S1 — Durable processing pipeline

**Scenario.** Today, `CompleteUploadHandler` commits, then writes a message into an in-process `Channel` (`src/NetDropbox.Application/Uploads/CompleteUpload.cs:82-84`, `src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs`). A single `ProcessingWorker` in the API process consumes it serially. `processing_jobs` rows are written and never read back. Every deploy loses the queue. An exception outside the worker's try block stops the host. Product now wants three stages per upload: virus scan (blocking downloads), thumbnail and text extraction (best effort), plus a customer bulk-import API that will push **500 k files** over a weekend while normal users keep working.

**Operating constraints.**
- Two API replicas today, five next quarter. Workers may run in the API process or separately; you decide.
- Postgres 16 is the system of record. RabbitMQ is already in `docker-compose.yml` but unused. Adding it is allowed if you justify it.
- Scan p95 must stay under 10 s for interactive uploads **during** the bulk import.
- Nothing may be scanned twice concurrently, and nothing may be lost on crash or deploy.

**Deliverable.** A design doc (≤ 2 pages) covering:
1. **Handoff:** the transactional outbox, or "the jobs table *is* the queue" (`SELECT … FOR UPDATE SKIP LOCKED`). Compare both, pick one.
2. **Delivery semantics:** at-least-once plus idempotent handlers. What's the idempotency key for a scan? What if a worker dies mid-scan (lease or visibility timeout)?
3. **Retries:** backoff without head-of-line blocking (see INC-002), classifying poison vs transient errors, the DLQ and who looks at it, and replay tooling.
4. **Fairness and backpressure:** the bulk tenant must not starve interactive users. Separate queues, priority, per-tenant concurrency caps?
5. **Stage separation:** a scan failure versus a thumbnail failure. What does the user see in each case?
6. **Observability:** queue depth, oldest-job age, attempts histogram, DLQ size, per-stage latency. Which alerts fire, at what thresholds?
7. **Rollout:** migrate from the Channel without losing in-flight work. Dual-run? Cutover flag? Rollback?
8. **How it's tested:** crash-injection tests, the concurrency test, a load test.

**Explain before touching.**
- What happens today if the process dies after `SaveChangesAsync` but before `EnqueueAsync`? Point to the line.
- What happens if Postgres commits but the RabbitMQ publish fails, and vice versa, in your design?
- Why not do the scan synchronously inside `complete`?

Answer: `training/_answers/ladder-S1.md`
