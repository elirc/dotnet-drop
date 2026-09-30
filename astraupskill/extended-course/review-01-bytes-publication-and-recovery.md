# Review 01: bytes, publication, and recovery

Attempt the twelve exercises before reading these answers. For arithmetic, write units and valid indexes. For storage, draw pending and published paths separately. For completion, use one column per resource. The most common mistake is to collapse several states into uploaded or completed and then infer guarantees that no single boundary provides.

## ND-01A: exact partitions

Zero bytes with chunk size four has one chunk at index zero with expected size zero. One byte has one chunk of size one. Eight bytes has two chunks of size four. Ten bytes has sizes four, four, and two. The positive-size formula subtracts one before division and adds one afterward, avoiding overflow from adding a rounding term to a large total.

The offset calculation widens the index before multiplication. Required chunk count is checked against int.MaxValue before conversion. Negative indexes and index equal to TotalChunks are rejected. Full credit includes both the mathematical sum invariant and the special empty-file protocol, rather than applying one informal ceiling formula to every case.

## ND-01B: syntax and content have different authorities

An uppercase hexadecimal hash of exactly sixty-four characters passes syntax validation and is stored lowercase. Surrounding spaces make the raw input fail the exact-length check before the constructor's trimming can help. Nonhexadecimal characters fail syntax. A valid hash for different bytes passes session creation but fails final content verification.

A declared MIME type is metadata, not proof of file format or safety. The session constructor does not inspect content. A complete upload and processing pipeline may add other checks, but the answer should not attribute those checks to this method without evidence.

## ND-01C: time is split across boundaries

The constructor uses system UTC time to set a twelve-hour expiry. IsExpired uses its supplied time and includes equality. Handlers pass their injected clock when checking the session. Complete checks open status and a nonempty file Id but does not independently reject wall-clock expiry.

A deterministic current test can inspect ExpiresAt and call IsExpired with values before, equal to, and after it. A future design might supply creation time through a factory parameter or clock abstraction. That would be a source change requiring compatibility review, not a feature already established by the handler clock.

## ND-02A: acceptance requires exact size and digest

Valid content is written to a pending file, validated, then moved to the stable path. Truncated or checksum-mismatched content is not published and its pending file is cleaned in finally. Oversized input is detected using a read that can exceed the expected boundary by one byte; that excess need not be written or included in the digest used for the rejected result.

The rejected result therefore should not be presented as a full-stream forensic hash. It is evidence of failed acceptance under the bounded read algorithm. The final stable chunk is the accepted artifact; transmission progress alone does not establish it exists.

## ND-02B: identical contenders can both succeed

Each writer has its own pending file. One publishes first. A later move encounters the stable path and inspects it. If size and digest agree, the later attempt can accept the same published content. If they differ, it conflicts without replacing the winner. The pending loser file is cleaned, while the accepted stable path remains.

The test should not assume task creation order determines the winner. It should assert a coherent final file and outcomes appropriate to identical or conflicting content. Handler metadata checks cannot replace this filesystem boundary because two first requests can both see no metadata row.

## ND-02C: publication can precede metadata

After successful file publication and failed SaveChanges, the stable file may exist while no chunk row was committed. A retry can encounter that file and validate it as identical, then attempt metadata saving. A reconciliation experiment should inspect both filesystem and database state rather than declaring the chunk absent from one observation alone.

Temporary-file tests can establish publication and overwrite behavior. Real database tests are needed for unique metadata constraints and concurrent save outcomes. EF InMemory does not prove SQL isolation or a transaction spanning the filesystem. A recovery policy must define how orphaned files are recognized safely.

## ND-03A: a preflight observes capacity

EnsureCanReserve checks without increasing UsedBytes. Reserve repeats the guard and then mutates usage. Early rejection avoids assembly and external blob work for known insufficient capacity. Two separate contexts can still each observe enough capacity before either saves a reservation; that schedule requires a persistence-level concurrency mechanism to resolve safely.

The subtraction guard avoids adding bytes to UsedBytes before determining capacity. It also rejects an already-overquota state. An answer that calls the early check a lock or reservation has assigned behavior absent from the method.

## ND-03B: failure after save does not erase commitment

After SaveChanges succeeds, the session can be completed, quota increased, and file/version/job records stored. If queue enqueue then fails, the caller can see failure while those facts remain. A sequential retry is rejected because the session is no longer open. This avoids a second normal completion but does not return a stable receipt or prove processing was delivered.

The ProcessingJob record is a potential recovery anchor. A proposed dispatcher or recovery scan needs explicit implementation and tests. Its mere presence is not proof that lost queue delivery is already repaired. The answer must distinguish recorded intent, delivered message, and completed processing.

## ND-03C: choose different evidence layers

A handler fixture can make the queue throw after a successful save and inspect a fresh context for completed state and one quota charge. It can establish the handler's ordering under those fake ports. A separate-context, real-provider test can exercise concurrent quota reservation or completion and verify the chosen database concurrency policy.

The first test is not inferior because it is smaller; it answers a different question. The report should name provider and fake boundaries so a reader can combine evidence appropriately. A generic statement that all upload tests pass obscures those distinctions.

## ND-04A: record obligations explicitly

A defensible ledger binds one accepted file/version receipt to the session and owner, includes the quota change and durable processing intent in a defined database transaction, and tracks cleanup separately. External blob bytes and queue delivery remain outside that local transaction. Current records can inform the design, but a recovery dispatcher and stable retry response are proposed additions.

The design needs a uniqueness or concurrency rule for acceptance. Otherwise two contexts can both pass an open-session check. State invariants should include at most one accepted completion receipt per session and a quota change corresponding to that accepted version under the chosen accounting policy.

## ND-04B: model delivery ambiguity

A lost response after commit should allow a retry to recover the same receipt without creating another version or reservation. A lost acknowledgment after queue delivery can cause a resend. The consumer therefore needs a stable processing identity or receipt that prevents duplicate effects even when messages repeat.

A pure model can establish its transition rules and reject a deliberately broken retry implementation. It cannot establish the real database's locking, queue guarantees, or process restart persistence. The evidence report should state those limits and name the next integration test.

## ND-04C: cleanup needs reference authority

Blob bytes saved before a failed database transaction may be unreferenced, but another successful file or active writer can share the content identity. Unconditional compensation deletion is unsafe. A reclamation design needs authoritative reference checks, coordination with active publication, and a policy for uncertain ownership or recent writes.

The capstone does not prescribe a universal retention interval. It requires a justified lifecycle rule and tests that prevent deleting shared or active content. A useful answer explains which actor owns reclamation and how it retries safely after its own failure.

## Rubric for the first loop

Assess arithmetic precision, resource-state reasoning, failure classification, and evidence scope separately. Strong submissions preserve the difference between a sent body, an accepted chunk, saved metadata, an accepted file version, and completed processing. They can use a small incident table to reject an attractive but incorrect claim of atomic completion across every resource.
