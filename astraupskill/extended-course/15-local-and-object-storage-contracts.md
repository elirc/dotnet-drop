# 15. Local and object storage are different implementations of an incomplete contract

The earlier upload chapters established the contract for receiving a chunk. Whole-blob storage introduces another boundary. Read [LocalBlobStorage](../../src/NetDropbox.Infrastructure/Storage/LocalBlobStorage.cs) alongside [S3CompatibleBlobStorage](../../src/NetDropbox.Infrastructure/Storage/S3CompatibleBlobStorage.cs). Both expose existence, save, open, and delete operations, but matching method names do not prove matching failure behavior. This chapter teaches how to turn the differences into explicit contract questions without modifying either adapter.

The prerequisite is the distinction between content identity and publication. A content hash names expected bytes. It does not itself prove that a file at the corresponding path contains those bytes, nor that a remote object upload completed. A service can calculate the correct name and still encounter a partial write, a stale object, or a failed read. Keep expected identity and observed storage evidence in separate columns of your notebook.

## Read the local save as a timeline

LocalBlobStorage computes a path, creates its directory, checks whether the file exists, and creates and copies into the final file only when it does not exist. It returns the supplied content hash. The implementation does not use the pending-file publication protocol described for chunk storage. Do not transfer the chunk adapter's guarantees merely because both implementations use a filesystem.

Consider a source stream whose first read returns four bytes and whose second read throws. The final blob path can have been created before the second read. After the exception, an existence query may return true for a file containing only those first bytes. A second save sees existence and skips the copy. This is a source-derived failure hypothesis suitable for a disposable filesystem fixture. It is not a report that a particular user's stored file is corrupt.

The distinction is operationally significant. A retry that equates existence with successful publication can preserve an incomplete artifact. Recomputing the intended content hash does not repair the artifact unless the retry actually reads and validates stored bytes or replaces them through a safe protocol. In a worksheet, label the file state as absent, receiving, or verified rather than reducing all three to a Boolean.

The existence check and creation are also separate operations. Two concurrent saves can both observe absence. What happens afterward depends on the filesystem operations and timing, including whether one open prevents another. A useful test records each operation and exception; it does not assume that the source provides a per-key lock. Even if one particular run rejects the second writer, that is not evidence of a deliberate contract to return an idempotent success to both callers.

## Existing objects are a policy decision

The local adapter skips consumption of the supplied stream when the target already exists. That can be an optimization when stored objects are immutable and verified, but those preconditions need an owner. Who establishes verification? Who can modify the storage root? How are interrupted writes distinguished from successful writes? A contract that says only "save returns a key" leaves these questions unanswered.

An exercise implementation might require an existing object's length and digest to match before treating the save as a replay. Another might publish only verified temporary objects and rely on a durable manifest. A third might delegate immutability to an object service with conditional creation. These are alternative proposals, not features of the inspected adapter. The right choice depends on the failure model and performance budget.

The S3-compatible adapter performs a PutObject operation for the supplied key. The visible request does not express the local adapter's skip-existing behavior as a conditional publication rule. Remote bucket configuration, object versioning, permissions, and service semantics can affect the result; none should be inferred from this class alone. Document the specific request built by the code, then list deployment facts that a real contract test would need.

This makes "both adapters passed save then read" a weak compatibility test. That happy path does not compare existing-key behavior, cancellation, stream ownership, partial writes, concurrent saves, or deletion of an absent object. A shared adapter suite should name those cases and permit intentionally different outcomes only where the abstraction explicitly allows them.

## Design a safe conformance fixture

Use an explicit new temporary directory for the local implementation. Populate it only with synthetic bytes. Supply a custom stream that throws after a known prefix, another that counts reads, and a third that blocks on a test-controlled gate. Avoid random sleeps: a gate lets the test stop after the final path is created and before the copy completes.

First assert a normal round trip. Then execute the throwing stream and observe the final path through the adapter's own read operation. Record whether the file is absent, partial, or complete, and preserve the exception category. Next retry with the complete stream and observe whether its reads occur. This sequence tests retry behavior after a real intermediate state rather than constructing an impossible state with reflection.

Keep cleanup scoped to the directory created by the fixture. A safe teaching harness should never accept a repository storage root as an implicit default. If a path already exists, fail before writing. A generated fixture can return its output path for manual inspection; it need not delete anything automatically. This makes the experiment reviewable and avoids turning a storage lesson into an accidental data migration.

An S3-compatible contract test needs an explicitly provisioned disposable service and credentials with a narrow scope. This workbook does not provision one or ask learners to run against a live bucket. A request-recording fake can still verify which bucket, key, stream, and cancellation token the adapter passes. Report that as request-construction evidence, not remote durability evidence.

## Keys, paths, and trust boundaries

LocalBlobStorage uses the first two key characters as a directory prefix and combines the supplied key into a path. The adapter itself does not establish that every supplied key is a hexadecimal digest. The normal upload flow calculates content hashes, while thumbnail processing uses its own generated key form. Therefore a proposed validation rule must accommodate all legitimate callers rather than assuming that every key has one format.

Do not turn this observation into an unsupported exploit claim. Instead, enumerate key producers and decide whether the storage abstraction accepts only internal validated keys or must defend against arbitrary caller input. An internal value object can centralize permitted formats; separate namespaces can distinguish immutable content from mutable derivatives. Either change would need compatibility tests for existing thumbnail naming and current stored objects.

Deletion has an equally important contract. A successful delete response may mean the object is now absent, not that this call removed a previously present object. That difference matters when reference accounting or audit records attach meaning to deletions. Test repeated deletion as an idempotency question and keep its answer separate from whether database references were updated.

## Exercise ND-15A: compare the adapter matrix

Create a matrix for absent-key save, existing-key save, failure after a prefix, repeated delete, and opening an absent object. Fill the cells supported by source and mark the cells requiring execution or deployment evidence. Explain why an existence method cannot stand in for a verified-publication method.

## Exercise ND-15B: specify the interrupted-copy fixture

Describe the stream, gates, temporary output guard, observations, and expected follow-up retry. Include an assertion that detects whether the retry consumes the replacement stream. State which outcome would reveal a difference from the chunk publication protocol.

## Exercise ND-15C: propose a storage contract revision

Choose either verified immutable publication or explicit replaceable objects. Define existing-key semantics, cancellation aftermath, caller stream ownership, key validation, and adapter-specific evidence. Include a migration concern for thumbnails and one performance cost. Keep the proposal separate from the current implementation.

## Review checkpoint

A defensible adapter review can describe what happened between two method calls. It knows that a Boolean existence result discards useful state, and it knows when the inspected request ends and remote-service evidence begins. The goal is a testable contract, not a claim that every backend can be made identical by renaming methods.
