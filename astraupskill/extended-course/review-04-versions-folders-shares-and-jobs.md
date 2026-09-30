# Review 04. Versions, folders, shares, and jobs

Use this review after completing chapters 08 through 11. Each answer separates a source observation from a proposed invariant. A good submission may choose a different product policy, but it must make that policy explicit and test the schedules that threaten it. Do not award credit merely for naming a design pattern without explaining where it applies.

## ND-08A: trace history and current identity

The first AddVersion creates version number one and selects it as current. The second creates number two and selects that version. RestoreVersion selects the existing first version; it does not append a third entry or renumber the collection. A subsequent AddVersion chooses one greater than the maximum historical number, so it creates number three even though version one was current immediately beforehand.

Each AddVersion and RestoreVersion sets the node's scan status to PendingScan. The inspected methods do not clear ExtractedText or ThumbnailPath as part of those transitions. Therefore the state trace must show current identity, history, scan status, and retained metadata separately. A trace showing only "version goes back to one" loses the distinction between a current pointer and the historical sequence.

Restoring is not uploading because it neither supplies new bytes nor creates a new FileVersion in this method. It also does not itself prove that processing has been scheduled. The service's restore path needs separate inspection for that effect. Full credit requires identifying these negative boundaries without turning them into an unsupported assertion about every possible caller in the system.

## ND-08B: design a stale-scan fixture

Pause worker A after selecting and reading V1. Add V2 and make it current. Allow worker B to process V2 if the fixture includes a second worker, then resume A. The worker's node-level MarkScanClean call can assign extracted text and thumbnail metadata associated with A's older version. Its message version identity does not automatically constrain the node mutation.

A minimal proposed rule would accept current-node metadata only if the processed version still equals CurrentVersionId at finalization. A stronger model stores results per version and derives the visible result through the current pointer. The latter preserves historical evidence and supports restoration without confusing it with current presentation. Both proposals require a concurrency-aware final check; checking once before a long decode leaves another race window.

The fixture should assert the relation between result provenance and current identity, not just Clean status. Historical evidence for V1 can remain valid while being inapplicable to V2. Deduct credit if the test assumes that whichever job finishes last must define the current file, or if it claims that an in-memory gate alone proves a database conditional update works across processes.

## ND-08C: protect collection ownership

IReadOnlyCollection restricts the operations visible through that interface. It does not change the runtime object into an immutable collection. If the property returns the actual List, a caller with knowledge of that runtime type can cast and mutate it. A disposable domain fixture can demonstrate the distinction by observing the runtime type and attempting a controlled mutation on synthetic data.

A proposed boundary can expose a read-only wrapper or a snapshot while preserving a private backing field for EF mapping. The choice has allocation and freshness consequences: a snapshot is detached from later mutations, while a wrapper reflects them but rejects collection modifications through its public surface. Tests should verify both the intended public API and the persistence mapping after any real implementation change.

This is an object-ownership observation, not automatically a remotely exploitable endpoint. A remote caller would need a reachable path that performs the inappropriate mutation. The review should name the trust boundary it actually tested. Full credit requires avoiding both extremes: declaring the interface immutable, or claiming an external exploit without tracing an external entry point.

## ND-09A: build a guard matrix

CreateFolder checks for an existing raw name in the owner's parent scope, then constructs a node whose name is trimmed. The method does not perform the same parent validation as Move. Rename uses the owned nondeleted node and the domain name guard but does not perform a sibling duplicate query. Move validates an optional destination as an owned nondeleted folder, then delegates the node mutation.

Copy reads an owned source with versions without the same nondeleted filter used by GetOwnedAsync. It requires a file, adds a blob reference, creates a new node and version, and marks the new node clean using source metadata. Its inspected path does not reserve quota or validate the destination through Move's parent check. Delete and restore change soft-deletion state; neither establishes a recursive tree operation merely by its name.

An appropriate shared invariant is that visible siblings have unique normalized names, if the product chooses that policy. That policy must cover create, rename, move, copy, and restore. A preflight query alone is insufficient under concurrency; a persistence constraint or another enforceable mechanism needs a matching normalization rule and conflict response. Accept alternative policies that intentionally permit duplicates, provided navigation and selection remain unambiguous.

## ND-09B: construct two graph counterexamples

A folder can be proposed as its own parent because the domain's self-parent rejection is conditioned on file type. An ancestor can also be moved beneath its descendant unless another guard detects the path. The service verifies the destination is an owned folder, which is necessary but does not prove acyclicity. Draw identifiers and directed parent edges to make this clear.

A proposed ancestor walk rejects a destination whose ancestor chain contains the moving node. It must also handle preexisting cycles safely, for example by tracking visited identifiers rather than looping forever. The walk's correctness under one snapshot does not settle concurrent moves. Two actors can each validate a graph before either writes, then jointly create a cycle.

A provider-backed test should coordinate two contexts so both validate before either commits. The proposed enforcement might serialize graph mutations within an ownership scope or use another database-supported strategy. The review need not prescribe one universal solution, but it must show how the chosen strategy prevents the joint schedule. A test with two moves executed sequentially does not exercise this concurrency problem.

## ND-09C: review copied scan evidence

The current copy path calls MarkScanClean on the new node after creating its version. That call does not derive cleanliness by rescanning the copied bytes. A matrix should therefore include source Clean, Pending, Failed, and Infected states and observe the same explicit clean transition in the inspected method, subject to earlier source-loading and file-type checks.

A proposed policy could reuse a clean scan only when it is tied to the exact immutable content and an acceptable processing profile. Otherwise the copy remains pending and receives its own work item. The proof should name the source version and blob identity; a node-level status without provenance is weaker evidence. Reject answers that copy a status label and assume that label alone validates bytes.

The test's scope matters. A direct service fixture can demonstrate which method is called and which state is saved. A hosted test establishes routing and authorization around that path. Neither should be described as a deployed incident. The exercise is about identifying the missing evidence and specifying a coherent copy contract before changing application behavior.

## ND-10A: compare download guards

Owner download selects an owned nondeleted node, verifies file type and scan status through its guard, and selects either an explicit version or the current version. Public download begins with a usable token, optional password verification, and a permission threshold. It then loads the linked node and requires Clean, but the inspected query does not add the owner's nondeleted predicate or the same explicit file-type guard.

Expiry is checked through ShareLink.IsUsable, where equality with the expiry instant is no longer usable. The owner route uses authenticated ownership rather than the share's password and permission fields. These mechanisms answer different authority questions, so the matrix should not mark absent share checks on the owner route as an automatic defect.

A consistent soft-deletion proposal can deny all new downloads of deleted nodes, including public links. That would require an explicit public-path check or shared admission policy. Another product might intentionally retain a share through trash, but it must say so. Full credit depends on matching the proposed policy to the actual branch that needs change, not merely recommending "more authorization."

## ND-10B: define an access metric

When metadata checks pass, the service calls RecordAccess and saves before opening the blob. If opening fails afterward, the access record can already be committed. It therefore measures an admitted or recorded access attempt, not completed delivery. Name a separate metric for completed transfers only if the implementation observes the appropriate response lifecycle and defines ranged and partial requests.

Concurrent increments require persistence evidence. Two contexts can read the same count, increment locally, and attempt writes. Whether one is rejected or a value is lost depends on the real concurrency protocol. The declaration of a concurrency token alone does not prove token advancement. A process-local test of one increment cannot establish exact global accounting.

The review should reward precise units: requests admitted, transfer attempts, bytes delivered, or full representations delivered. A single counter cannot silently switch among these meanings. If the product uses a count for billing or a hard download limit, its enforcement and audit requirements are stronger than those of an approximate dashboard metric.

## ND-10C: specify revocation semantics

For admission-only revocation, a request that passes usability checking before revocation may continue, while a later request is denied. A controlled fixture pauses after admission, revokes in another actor, then resumes the first request. That outcome is coherent if explicitly documented. It does not require the service to interrupt an already-open stream.

Active-transfer revocation is stronger. It needs a mechanism that observes revocation during transfer, such as a cancellable transfer registry or periodic authorization checkpoints, plus a policy for already transmitted bytes. A one-time IsUsable call cannot provide that behavior. The review should reject claims that saving Revoked automatically recalls bytes already delivered.

## ND-11A: trace attempt states

The normal sequence is Queued with zero attempts, Running with one, Failed after the first failure, then Running with two and Failed, then Running with three and DeadLettered. MarkFailed classifies based on Attempts. The methods themselves do not enforce a complete transition graph. Calling MarkRunning after completion can change the state and increment attempts again.

The worker also lacks a visible completed-job early exit in the inspected path. Therefore duplicate delivery needs explicit consideration. Job completion is not synonymous with clean content: the infected-content branch can complete the processing job because the scan operation reached a terminal result. A rubric should require all four concepts: delivery, attempt, job result, and content classification.

## ND-11B: design durable admission

A proposed persisted work item is authoritative, while the channel can be a wake-up optimization. Workers claim eligible records conditionally, record an ownership generation, and finalize only while that claim remains valid. Completed work returns its existing outcome or is ignored. A recovery query discovers eligible or expired claims after a process restart.

Stable external-effect identity is still required because a worker can publish an artifact and lose its acknowledgment. Durable admission does not make every downstream effect exactly once. Full credit requires explaining the artifact retry and cleanup policy rather than ending the answer at "use a database queue."

## ND-11C: classify cancellation failures

Initial lookups occur before the worker's main try region, so a cancellation there does not follow the same catch path as a cancellation during sample reading. Inside the handled region, the catch attempts failure-state persistence and possible requeue with the same token. An already canceled token can prevent those recovery calls from completing.

A gated stream fixture can establish this path without simulating an entire host shutdown. It should record the phase, token state, attempted recovery calls, and final observed state. Full host behavior also depends on hosting lifetime and persistence, so the fixture's report must retain that limitation. Award the strongest grade to answers that preserve both the original cancellation and any recovery failure as distinct evidence.
