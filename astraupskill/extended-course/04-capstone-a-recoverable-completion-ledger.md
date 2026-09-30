# 04. First capstone: a recoverable completion ledger

The first Netdrop learning loop ends with a proposed recovery design for upload completion. The current handler validates bytes and prepares several resources in sequence, but a failure after database saving can leave the caller uncertain and queue delivery unfinished. Your capstone is to define a durable completion ledger and recovery protocol that makes those states inspectable. It is a design and disposable-model exercise; application behavior remains unchanged.

Anchor the proposal in [CompleteUploadHandler](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs). Its actual order is session validation, chunk-index validation, quota preflight, assembled-content verification, blob lookup or saving, file/version/job construction, quota reservation, session completion, database save, chunk cleanup, and queue enqueue. Your design must identify which steps move inside a durable boundary and which remain external.

## Define what completed means

A single completed flag can hide several facts: the bytes were verified, the file version was committed, cleanup was attempted, and processing was scheduled or delivered. Choose explicit state dimensions rather than one vague status. A useful ledger might record the accepted file/version identity and a durable processing intent, while cleanup is tracked as retryable housekeeping.

Do not require every external action to finish before the client can learn that its file version exists unless the product needs that stronger promise. A receipt can confirm durable acceptance while indicating processing remains pending. Conversely, do not label a file fully processed merely because a queue message was sent. The consumer's outcome is another boundary.

The receipt should be keyed by the upload session and owner scope under a defined authorization policy. A sequential retry of a completed session could return the stored result rather than attempting another reservation. This is a proposed improvement over current rejection of a nonopen session. It must preserve the rule that a retry cannot create a second version or double-charge quota.

## Put intent beside the committed graph

One design candidate saves a processing-delivery intent with the file version, quota change, and completed session. A dispatcher later sends messages from that durable intent. If the process fails before dispatch, the intent remains discoverable. If it fails after sending but before acknowledgment, the dispatcher can resend, so the consumer needs a stable identity and idempotent handling.

The current ProcessingJob record is a useful source fact to inspect, but its existence alone does not prove an outbox dispatcher or recovery scan is implemented. The capstone must define the actual delivery protocol rather than renaming an existing row and assuming reliability follows. Identify ownership, retry count, terminal failure handling, and how a stuck job becomes visible to an operator.

Chunk cleanup should not be the only path to dispatch. In the current order, cleanup failure prevents the later enqueue call. A proposed design can separate cleanup from processing intent so one housekeeping problem does not erase the obligation to process an accepted version. That change needs tests for both resources and a clear retention policy for abandoned chunk data.

## Handle external blob uncertainty

Blob saving occurs before database commit in the current path when content is new. If saving succeeds and the database transaction fails, external bytes may remain without the intended committed reference. A recovery design can use content-addressed storage plus reconciliation, an ownership receipt, or another explicit mechanism. It should not delete bytes merely because one request failed.

Two requests may refer to the same content hash. Deleting a blob during compensation without checking committed references can break another successful file. The cleanup decision therefore needs authoritative reference evidence and a race-safe policy. A disposable model can represent that ownership decision, while a real implementation needs storage and database integration evidence.

Define a grace period or quarantine state only if the product and storage lifecycle justify it. The capstone does not require a specific number of hours. It requires a reasoned rule that prevents an active writer or committed reference from being mistaken for garbage. Operational constants should follow an explicit policy, not an arbitrary example copied into production.

## Required incident table

Include successful commit with lost response, database failure after blob save, cleanup failure after commit, queue delivery followed by lost acknowledgment, and two concurrent completion attempts. For each, list durable receipt, quota, file version, blob reference, cleanup state, and delivery intent. State what a retry should return and which actor owns recovery.

For concurrent completion, a domain status check in two separate contexts is not enough. Propose a database concurrency or uniqueness mechanism that chooses one accepted completion. The losing attempt should recover the existing receipt or receive a defined conflict without creating another version. Tests must use separate contexts and the real provider if they claim that guarantee.

## Exercise ND-04A: design the ledger

Write the state fields and invariants for accepted version, quota reservation, processing intent, and cleanup. Draw the transaction boundary and label external resources. Explain which current records can support the design and which new behavior still needs implementation.

## Exercise ND-04B: prove retry classification

Specify a disposable state model for a lost response and a repeated completion. Assert one accepted version and one quota reservation. Add a queue acknowledgment failure and show why duplicate delivery must not create duplicate processing effects. Keep model evidence separate from real database and queue evidence.

## Exercise ND-04C: review cleanup safety

Propose a policy for blob bytes saved before a failed database commit. Include another file that references the same content and an active writer. Explain why unconditional deletion is unsafe and identify the authoritative evidence needed before reclamation. Do not run cleanup commands or modify stored data.

## Completion rubric

The first learning loop is usable when you can derive chunk boundaries, trace publication, explain completion's resource states, and defend this recovery design. The capstone passes when every incident has a defined receipt and recovery owner. It fails if quota preflight is treated as a reservation, a database save is treated as a distributed transaction, or a queue send is treated as proof of exactly-once processing.
