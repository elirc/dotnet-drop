# 18. Capstone: make processing results belong to a version

This capstone asks you to design and prototype a version-aware processing path in a disposable workspace. It builds on [FileNode](../../src/NetDropbox.Domain/FileNode.cs), [ProcessingJob](../../src/NetDropbox.Domain/ProcessingJob.cs), and [ProcessingWorker](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs). The application is not changed by this workbook. Your deliverable is a small model, explicit tests, and a migration proposal that can be reviewed before anyone considers production implementation.

The problem is precise: the current worker processes a version identified by a message but writes scan status, extracted text, and thumbnail metadata on the node. The inspected path does not first establish that this version remains the node's current version. A result can therefore be valid for the bytes processed while being unsuitable as the current node's presentation. Treat validity and applicability as separate predicates.

## State the product policy before changing fields

Choose a policy for historical versions. One reasonable proposal stores a processing result for every version and lets the node's current pointer select the visible result. A historical clean result remains useful if the owner restores that version. Another policy discards historical derivatives to save storage and schedules processing again on restore. Either can be coherent; mixing them without a declared rule cannot.

For this capstone, use the first policy. A processing result is identified by version and processing profile. The profile represents the algorithm or configuration that produced the result. A version processed under an older profile may need work again even though its bytes are unchanged. This is a proposed extension, and the current model does not already provide this result identity.

Define the visible state as a projection: select the current version, then find its applicable result. If no applicable result exists, display pending. If the result is clean, expose its text and derivative references. If infected or failed, apply the chosen download policy. Do not copy old text into a new version's visible fields merely because it was convenient to keep it on the node.

## Work the stale-worker schedule

Use two versions V1 and V2. Worker A reads V1 and pauses after reading its bytes. The owner creates V2, making it current. Worker B processes V2 and stores a clean result. Worker A then resumes and stores a result for V1. Under the proposed projection, the current view continues to select V2's result. V1's successful processing does not overwrite V2's meaning.

The important assertion is relational, not just a final enum value. Assert that the visible derivative's version identifier equals the current version identifier. An implementation that ends with Clean for the wrong version fails even if every individual job reports success. Test the reverse completion order as well, because relying on arrival order is the defect you are trying to remove.

Now introduce a restore. The owner restores V1 after both results exist. The projection can expose V1's applicable result immediately under the chosen policy. If the processing profile changed, it instead reports pending for the new profile. This trace forces the model to distinguish version identity, result identity, and current selection. A timestamp comparison alone is not enough.

## Define work identity and claiming

A message should identify the work, not become the only record that work exists. Propose a persisted work item with a unique identity for version and profile, a state, attempts, and a claim generation. A dispatcher can notify workers, while a recovery query can discover eligible persisted work after a restart. This is stronger than merely replacing an in-memory channel with a differently named abstraction.

Claims need a concurrency rule. Two workers may discover the same eligible item. A conditional transition from eligible to running can choose one owner, provided the actual database enforces the comparison. If claims expire, a later worker receives a newer generation. The older worker must not finalize the item after losing ownership. This generation check is often called fencing, but the useful idea is simply that stale authority must be rejected.

Do not hold a database transaction open while decoding a large image. Read or claim the work in a short transaction, perform external work, then finalize conditionally. This introduces intermediate artifacts, so the design must track them. The database cannot automatically roll back a thumbnail object written to external storage. A deterministic artifact key or an artifact ledger can make retries and cleanup tractable.

## Make duplicate delivery harmless by contract

A duplicate message for completed work should return the existing outcome or do no additional business work. It should not increment Attempts merely because a duplicate notification arrived. The current ProcessingJob methods do not enforce that full protocol, so implement it in the disposable model rather than pretending a method rename solves the problem.

Separate attempt identity from work identity. An attempt is one execution under a claim. A work item may have several attempts but one accepted result for the selected profile. If a stale attempt produces a derivative, its artifact may exist without being accepted. Record enough information to reclaim it later without deleting a derivative referenced by a successful attempt.

The failure budget also needs definition. Does cancellation count as an attempt? Does malformed input count as retryable? Is an unavailable blob store different from a permanently unsupported image? The capstone should classify at least transient infrastructure failure, permanent content rejection, cancellation, and stale claim. A single catch-all retry policy obscures those decisions.

## Build the prototype in four increments

First create a pure state model with versions, results, current selection, and work items. Use fixed identifiers and a manual clock. Demonstrate the stale-worker schedule without storage or threads. This establishes the intended relationships and makes the failure easy to understand.

Second add a controlled scheduler that interleaves claim, external work, and finalize operations. It should be possible to pause exactly before finalization. Assert that a stale generation cannot replace an accepted result. A deterministic scheduler is more useful here than launching many tasks and hoping the race occurs.

Third add a fake artifact store that can succeed and then throw a simulated lost-acknowledgment exception. The retry must not assume the artifact is absent simply because the call failed. Decide whether deterministic keys or explicit verification establish reuse. Record any orphan as a cleanup obligation rather than silently forgetting it.

Fourth write the database migration and integration plan without applying it to this repository. Include uniqueness, conditional claims, result references, and backfill rules. Existing node-level metadata may have uncertain version provenance. A conservative migration can mark ambiguous results for reprocessing instead of assigning them to a version without evidence.

## Acceptance evidence

The minimum suite includes two workers for one work item, duplicate notification after completion, process loss after artifact publication, stale finalization after claim renewal, restore of a previously processed version, and profile change. Every test should assert both accepted state and side-effect obligations. Counting green tests without describing these schedules is not sufficient.

A design review should also inspect accessibility and user explanation. Pending does not mean the upload failed. A historical result does not necessarily describe the current version. A clean result under an obsolete profile may be awaiting reprocessing. The API and UI should communicate these distinctions using stable states rather than exposing internal exception messages.

Keep the prototype bounded. It need not include a real antivirus engine, a distributed scheduler, or a cloud account. Its purpose is to make the state protocol and recovery obligations concrete. Report the absence of provider-backed concurrency evidence as a limitation, then specify the smallest integration experiment that would close that gap.

## Exercise ND-18A: write the result projection

Define the inputs and output of the current-version projection. Work V1 and V2 through out-of-order completion and a later restore. Include a profile change and show why the latest completion timestamp cannot replace version identity.

## Exercise ND-18B: reject a stale finalizer

Describe a two-worker schedule in which the first claim expires and the second worker completes. Specify the conditional write that rejects the first worker's late result and the cleanup obligation for its external artifact. Identify the database evidence needed to enforce the condition.

## Exercise ND-18C: submit a capstone review packet

Produce a state diagram, six deterministic tests, an artifact accounting table, a migration sketch, and a limitations section. Explain which current behavior the model reproduces and which behavior it deliberately replaces. Keep all implementation work outside the application tree.

## Review checkpoint

The capstone succeeds when a result cannot silently change the meaning of another version, duplicate work has a defined outcome, and external artifacts remain accountable after failures. An elegant class diagram without those schedules is incomplete. A small executable protocol with clear limits is a stronger submission.
