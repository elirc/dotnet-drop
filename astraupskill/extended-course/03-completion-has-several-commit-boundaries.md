# 03. Completion has several commit boundaries

CompleteUploadHandler assembles bytes, validates them, prepares database state, saves changes, removes chunk files, and enqueues processing. The word complete covers several operations with different failure behavior. This chapter builds a state ledger that follows those operations in order and distinguishes a read-only quota preflight from an actual reservation. It is a foundation for recovery design, not a claim that the current workflow is already crash-atomic.

Read [CompleteUploadHandler](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs), [UserStorageAccount](../../src/NetDropbox.Domain/UserStorageAccount.cs), and [the handler tests](../../tests/NetDropbox.Tests/Uploads/UploadHandlerTests.cs). The tests use fresh EF InMemory contexts and fake external ports. They establish selected application outcomes but do not reproduce every SQL transaction, filesystem, queue, or cross-process race.

## Establish the prerequisites first

The handler looks up the session by Id and OwnerId, then rejects a nonopen or expired session. It reads chunk indexes in order and requires both the expected count and an exact sequence from zero through TotalChunks minus one. Count alone is not enough: duplicate or missing positions could produce the right number of records while describing the wrong byte partition.

Next it loads the user and calls EnsureCanReserve for the session's total size. This method checks capacity without modifying UsedBytes. The guard uses subtraction after ensuring UsedBytes is not already beyond quota, avoiding a potentially overflowing UsedBytes plus bytes comparison. A failed preflight therefore leaves the account's usage unchanged and prevents later assembly and blob work in this path.

Preflight is a statement about the account state observed now. It is not a reservation that excludes another completion. Two operations can each pass a read-only capacity check before either changes usage. A durable concurrent quota guarantee needs an atomic persistence boundary for the later reservation and related writes. The existence of this early check does not establish that boundary by itself.

## Validate the assembled artifact

The handler opens an assembled stream and calculates both SHA-256 and length. It rejects a length mismatch before a hash mismatch. Both checks matter because the session's promise includes a byte count and a final content identity. A fake chunk provider in a handler test can return an assembled stream of the wrong length even if chunk metadata looks complete, making this boundary independently testable.

After successful validation, the stream position is reset to zero for blob saving. A test double that ignores the stream position can miss this requirement. In a real storage adapter, failing to rewind would save an empty or partial stream after hashing consumed it. This is an example of a stateful interface contract hidden behind a type as familiar as Stream.

The handler looks for a blob by content hash. If none exists, it calls external blob storage and adds a new BlobObject. If one exists, it increments the reference count. These branches have different external work. A correct explanation should not say every completion writes new blob bytes, nor should it assume a hash lookup alone provides a distributed uniqueness guarantee under concurrent first creation.

## Prepare the database graph

The handler finds a nondeleted file with matching owner, parent, and name or creates one. It adds a version, creates a queued virus-scan ProcessingJob record, reserves quota, and marks the session completed with the file Id. These are object and context mutations before SaveChanges. Their durability depends on the database save boundary and actual provider behavior.

UserStorageAccount.Reserve calls EnsureCanReserve again before increasing UsedBytes. This second check is useful within the observed object state, but it is not automatically a cross-context compare-and-swap operation. If two contexts each hold an old account value, domain guards alone do not prove the database prevents a lost update or over-allocation. That question belongs to mapping, transaction, and concurrency evidence.

The handler then awaits SaveChanges. Only after that succeeds does it call DeleteSessionAsync and EnqueueAsync. This ordering is central to recovery. A queued database job record and a delivered queue message are different facts. Creating one does not prove the other has happened.

## Work a post-save failure

Imagine the save succeeds, chunk cleanup succeeds, and queue enqueue fails. The database can contain a completed session, a file version, a blob reference, a quota reservation, and a queued job record. The caller may see failure even though those state changes are committed. Retrying the same completion reaches the nonopen-session guard and is rejected under current behavior.

This does not mean the earlier successful-save test is wrong. That test establishes normal completion and that a later sequential retry does not double-charge. It does not establish recovery from a queue failure after save. The distinction between preventing duplicate completion and returning a stable completion receipt is important: current retry rejection can avoid a second charge while leaving the client uncertain about the first result.

Now move the failure earlier, after blob SaveAsync but before database SaveChanges. External bytes may exist while the database graph has not committed. A compensation or garbage-collection policy might be needed, but deleting a blob blindly could be unsafe if another operation references the same content. Recovery needs ownership and reference evidence, not just a catch block that reverses every apparent action.

## Build a failure matrix with separate resources

Use columns for chunk files, external blob bytes, database session state, file/version records, quota usage, job record, and queue message. Add rows for preflight rejection, assembled-length rejection, blob-save failure, database-save failure, cleanup failure after save, and enqueue failure after save. For each cell, write what the source establishes and what remains provider-dependent.

Avoid filling unknown cells with a comforting rollback assumption. EF InMemory test behavior is not evidence of SQL isolation, and a database transaction does not ordinarily include arbitrary filesystem and queue operations. The useful output is a map of recovery responsibilities. It tells a future implementer which failure windows need durable intent, retry, reconciliation, or compensation.

A proposed outbox could persist a delivery intent with the completed database graph and dispatch it later. That would change the failure model, but it still needs idempotent delivery or consumer handling because dispatch can succeed before acknowledgment is recorded. The capstone will ask for a bounded recovery design rather than claiming exactly-once external processing through a single flag.

## Exercise ND-03A: distinguish preflight and reservation

Trace UsedBytes through a failing preflight and a successful completion. Explain why EnsureCanReserve is read-only and why Reserve occurs near the database graph mutation. Give a two-context schedule showing why two successful preflights are not proof of safe concurrent quota allocation.

## Exercise ND-03B: write the post-save incident

Describe the state after SaveChanges succeeds and queue enqueue fails. Predict the current handler's response to a sequential retry of the completed session. Identify which durable record could support recovery and which receipt behavior would require a new design. Do not turn retry rejection into a claim that the original completion did not happen.

## Exercise ND-03C: choose two discriminating tests

Specify one handler test with fake external ports and one storage-aware integration test. The first should prove an ordering or persisted-state claim under a controlled failure. The second should address a concurrency or transaction question the EF InMemory fixture cannot establish. State exact expected observations and keep them separate from proposed guarantees.

## Review checkpoint

A strong answer names every relevant resource and commit boundary. It preserves the benefit of early validation while refusing to call it a distributed transaction. It can explain why a request may fail after durable state changed, and why recovery requires more than repeating the same command or blindly deleting external data.
