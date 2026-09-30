# 19. Capstone: quota and retention as accountable obligations

This capstone joins upload admission, storage accounting, and retention without pretending they already form one transaction. Read [UserStorageAccount](../../src/NetDropbox.Domain/UserStorageAccount.cs), [BlobObject](../../src/NetDropbox.Domain/BlobObject.cs), [FileService](../../src/NetDropbox.Application/Files/FileService.cs), and [JanitorWorker](../../src/NetDropbox.Infrastructure/Processing/JanitorWorker.cs). The proposed deliverable is an accounting model and reconciliation plan in a disposable workspace. No application data is migrated or deleted by the course.

The motivating distinction is between logical usage and physical storage. A user's quota may count logical file versions, current versions, or some other policy. A physical content object may be shared through deduplication. Releasing one user's logical charge does not necessarily make the physical object unreferenced. Conversely, an orphaned physical object may consume storage without belonging to any current user charge.

## Choose and document a charging unit

For the exercise, choose logical retained version bytes. Each retained version creates one charge for its owner, even if its content hash matches another version. This avoids making a user's quota depend on whether another user happened to upload identical bytes. It is a proposed policy; inspect the existing completion and copy paths before claiming they already implement it consistently.

A charge should have an identity tied to its cause, such as a version identifier. A repeated completion must not create a second charge for the same version. A release should refer to the charge it reverses. Avoid relying only on an aggregate UsedBytes field, because an aggregate cannot explain which events produced it or whether a repeated release was already applied.

The aggregate remains useful as a fast admission value. The proposal is to derive or maintain it from identifiable obligations under a concurrency rule. If the aggregate and ledger diverge, reconciliation can compare them. Without identifiable entries, reconciliation has to guess whether a discrepancy represents a missed charge, duplicate release, or different policy interpretation.

## Admission and reservation are separate

The current domain preflight EnsureCanReserve validates a prospective amount without changing UsedBytes. Two callers can both pass a check based on the same available capacity. This is not a reservation and should not be described as one. A proposed reservation must create state that other admissions observe and that a failed or expired operation can release.

Work an account with quota ten units and used six. Two sessions each request three. Independent preflights both see four available. Under a reservation policy, only one reservation can succeed if the database condition compares committed usage plus active reservations with quota atomically. A process-local lock would not prove this property across two API processes.

The reservation has its own lifecycle: active, consumed by a committed version, released after explicit cancellation, or expired under a defined policy. Consuming it should establish the charge without temporarily counting both forever. A lost response after consumption must allow a retry to discover the existing result. These are state transitions, not just arithmetic helpers.

Expiry complicates races. A janitor may decide that a reservation is old while a completion is in progress. A conditional transition must choose whether completion consumes it or cleanup releases it. If cleanup wins, completion needs a defined response or a new admission attempt. If completion wins, cleanup must not release the resulting charge. Use a version or state comparison backed by a real persistence mechanism.

## Retention is not immediate physical deletion

Soft deletion marks a node and records a time. The current janitor later removes old deleted nodes, while its inspected code does not explicitly release quota or decrement blob references. The capstone must decide where those obligations live. Deleting metadata first and hoping another component infers every lost reference is fragile unless the required evidence survives.

Propose a retention operation identified by node and retention generation. It records the versions selected for removal, associated charges, and content references before destructive work. A transaction can remove metadata and record release obligations together. External object deletion happens later after reference accounting confirms eligibility. This is a proposal for accountable cleanup, not a claim about an existing outbox.

Physical deletion needs a fresh check. Between an earlier count and the deletion attempt, another operation might create a reference to the same content object. A safe design can prevent new references while an object is being retired, use a conditional state transition, or adopt another enforceable protocol. Merely observing a count of zero once is not enough to justify deleting an object under concurrent creation.

## Work the repeated-cleanup incident

Suppose metadata removal and charge release commit, but the cleanup worker crashes before recording that it finished. On retry, the operation identity lets it find the committed release rather than subtracting the same bytes again. The physical-delete obligation may still be pending and can proceed independently if its reference condition remains valid.

Now suppose physical deletion succeeds but its acknowledgment is lost. A retry should treat absence as an acceptable terminal condition for that deletion obligation. It should not reconstruct missing metadata or release quota again. This is why each effect needs its own identity and status. One overall Boolean called cleaned cannot explain which part remains.

Finally, suppose a charge exists for a version whose metadata is absent, but there is no retention operation. Reconciliation should classify this as an unexplained discrepancy and surface it for policy-driven repair. Automatically subtracting every unexplained charge could hide a data-loss incident or a delayed transaction. A repair plan should preserve evidence and support a dry-run report.

## Reconciliation as a read-only first step

Begin with a function that computes expected logical usage from synthetic retained versions and compares it with synthetic charges and aggregates. Return categories: missing charge, duplicate cause identity, released charge still counted, aggregate mismatch, and ambiguous orphan. Do not perform writes in the first implementation. A dry-run result is easier to review and test.

Add physical accounting separately. Compare version references with blob-reference records and object inventory. A missing object with live references is different from an unreferenced object. The former is a data-availability incident; the latter may be a cleanup candidate after a safety interval and policy checks. Treating both as a number mismatch loses the information needed for safe action.

Use bounded arithmetic throughout. Quota comparison should avoid overflowing an addition when values are near the numeric limit. Release logic should reject or classify invalid inputs under an explicit contract. Test zero, exact fit, one beyond capacity, repeated release, and very large values. Passing ordinary three-byte examples does not establish correctness at the boundary.

## Exercise ND-19A: define a charge ledger

Specify identities and states for reservation, charge, release, and physical-delete obligation. Work one successful upload, one abandoned upload, and one repeated retention attempt. Explain which records support a safe retry and which aggregate values are merely summaries.

## Exercise ND-19B: resolve completion versus expiry

Write a two-actor schedule in which completion and cleanup both observe an active reservation. Define the conditional transition that selects one winner, the loser's response, and the observations required after a lost acknowledgment. Distinguish cross-process enforcement from an in-memory demonstration.

## Exercise ND-19C: build a dry-run reconciler

Use a small synthetic dataset containing a missing charge, duplicate cause, aggregate mismatch, live reference to a missing object, and unreferenced object. Produce a report with classifications and proposed next checks. Do not delete or repair anything automatically. Supply a rubric explaining when the report is safe enough for human review.

## Review checkpoint

Good accounting names the thing being counted and the event that changes it. Good cleanup preserves enough evidence to repeat an operation safely. The capstone is complete when a reviewer can trace a logical byte charge and a physical reference independently through admission, completion, retention, and interrupted cleanup.
