# 08. Version history is not scan history

A file can retain several versions while exposing only one as current. Netdrop models that distinction with FileVersion records and FileNode.CurrentVersionId. It also stores ScanStatus on the node. These choices create useful questions about which bytes a scan result describes and what happens when the current version changes while processing is underway. This chapter derives those questions from source rather than assuming every status belongs to the same identity.

Read [FileNode](../../src/NetDropbox.Domain/FileNode.cs), [FileVersion](../../src/NetDropbox.Domain/FileVersion.cs), and [FileService](../../src/NetDropbox.Application/Files/FileService.cs). A FileVersion has its own identity, a file identity, blob identity, version number, size, MIME value, hash, and uploader identity. FileNode holds the current-version pointer, scan status, extracted text, and thumbnail path. These are distinct levels of state with different lifetimes.

## Adding a version changes the current view

AddVersion rejects folders, computes the next number from the maximum existing version number, creates a version, appends it, changes CurrentVersionId, and sets ScanStatus to PendingScan. The earlier versions remain in the collection. The operation does not replace their content records. A version number is therefore a historical sequence label, while CurrentVersionId chooses which record the node currently presents.

The calculation assumes the in-memory collection represents the relevant existing history. Completion loads Versions before calling AddVersion. A direct call on an object with an incomplete collection could derive a different next number. The database's unique index on file identity and version number provides another boundary, but a domain calculation alone does not prove concurrency-safe numbering across contexts.

Two contexts can each load maximum version three and propose version four. The unique constraint can reject a duplicate number, but the application still needs to classify and recover from that conflict. A constraint prevents one invalid stored shape; it does not automatically decide which retry should return a receipt or how earlier external blob work is reconciled. Keep prevention and recovery as separate requirements.

## Restoration moves a pointer

RestoreVersion verifies that the requested version belongs to the node's current collection, assigns its Id as current, marks the node PendingScan, and touches its timestamp. It does not create a new FileVersion. Historical version numbers therefore do not increase merely because a user chooses an older version. A user interface should distinguish restoring a previous revision from uploading a new one.

The service loads the version collection and saves the changed node, but its RestoreVersionAsync method does not enqueue a processing message. That is a concrete observation of this path. Since the domain sets PendingScan, a proposed workflow must explain how scanning is resumed or whether another mechanism handles it. Do not claim an automatic scan was delivered merely because the status says pending.

Restoration also does not clear ExtractedText or ThumbnailPath in this domain method. Those fields may still describe the previously current content until another operation updates them. Whether a client displays them while PendingScan is a separate presentation question. The state model allows the distinction, so tests should not assume setting a status automatically erases derived metadata.

## Scan status belongs to the node in this model

MarkScanClean sets the node status and replaces extracted text and thumbnail. MarkInfected and MarkScanFailed change status but do not clear those metadata fields. A status transition and metadata invalidation are therefore separate behaviors. A strong state trace records both, especially when versions have different content types.

Imagine version one is text and has extracted words. Version two is an image. Adding version two makes the node pending but leaves version one's extracted text until processing updates it. If a search query reads ExtractedText without filtering pending status, it can observe metadata from an earlier current version. This is a source-derived consistency scenario to test, not a claim that all deployed clients expose it identically.

A proposed stronger model could associate scan status and derived artifacts with FileVersion instead of only FileNode, then project current-version state. That design would make the identity relationship explicit. It also requires migration, access rules for historical downloads, and a policy for reusing prior scan evidence. Moving one property is not enough to establish the complete guarantee.

## Work a stale processing result

The processing worker loads the job, node, and version named by a message. It uses that version's blob bytes, then calls node.MarkScanClean or another node-level status method. The inspected path does not compare message.FileVersionId with node.CurrentVersionId before applying the result. If another upload changes the current pointer, an older job can still update node-level metadata under this control flow.

Build a paper schedule: version one is queued; version two becomes current and pending; version one's worker finishes and marks the node clean using version one's content. The source identifies the potential mismatch. A deterministic integration fixture would need gates around the worker's read and publication points to reproduce it with actual persistence. A sleep alone would not establish the required ordering.

The proposed acceptance rule might require applying node-level results only when the processed version is still current. Historical scan evidence could still be stored against version one. Another design could retain all per-version results and derive the current view. Both are improvements to evaluate, not features this chapter installs.

## Read-only interfaces do not imply immutable collections

Versions is exposed as IReadOnlyCollection but returns the private List object. An ordinary caller cannot call Add through the interface, yet a runtime cast can reveal the concrete list. The property therefore restricts the declared API surface without creating a detached immutable snapshot. This is a useful ownership observation for domain tests and future encapsulation work.

Do not jump from that observation to a universal cloning requirement. EF mapping deliberately uses field access for the navigation. A stronger public collection boundary must preserve persistence behavior and avoid accidental duplicate collections. A read-only wrapper or snapshot can be considered, with tests for both external mutation resistance and ORM materialization.

The same principle applies to returned entities. FileService usually projects DTOs for reads, which reduces direct exposure through those methods. The domain's public property still matters for tests and other internal callers. Trace the actual exposure path before making a claim about an HTTP caller's ability to mutate memory.

## Exercise ND-08A: trace history and current identity

Create versions one and two, restore one, then add another version. Predict current identity, historical collection, next version number, and scan status at every step. State which fields restoration leaves untouched and why restoration is not the same operation as uploading a new version.

## Exercise ND-08B: design a stale-scan fixture

Write a gated schedule where an older version's processing finishes after a newer version becomes current. Identify the current worker statements that allow node-level state to be updated. Specify a proposed acceptance rule and distinguish historical scan evidence from current display state.

## Exercise ND-08C: protect collection ownership

Demonstrate the difference between an IReadOnlyCollection reference and an immutable collection in a disposable domain fixture. Propose a public boundary that preserves EF field mapping. Explain why a source-level cast observation is not automatically proof of remote exploitability.

## Review checkpoint

A strong answer keeps file identity, version identity, blob identity, and scan evidence separate. It can explain stale metadata without assuming every operation clears it, and it designs a test around the exact moment a result becomes authoritative. It treats history and current presentation as related but distinct contracts.
