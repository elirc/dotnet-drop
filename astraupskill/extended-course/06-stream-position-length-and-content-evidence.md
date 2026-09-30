# 06. Stream position, length, and content evidence

Streams carry both bytes and mutable position. A method that hashes a stream changes what the next consumer reads unless the stream is rewound or replaced. Netdrop's completion path explicitly resets the assembled stream after hashing. This chapter studies that interface contract, the difference between observed and declared length, and the evidence needed before content-addressed storage can be trusted.

Read [Hashing](../../src/NetDropbox.Application/Common/Hashing.cs), [FileSystemChunkStorage](../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs), and [CompleteUpload](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs). Hashing reads until the stream returns zero, updates SHA-256 incrementally, and accumulates a checked long length. It does not rewind the stream on entry or exit and does not dispose the supplied stream. The caller owns those lifecycle decisions.

## Hash from the current position

If a stream begins at position zero, Sha256WithLengthAsync observes the remaining full content. If it begins halfway through a seekable stream, the method hashes only the suffix it reads. Its returned Length is the number of bytes observed during that invocation, not necessarily the stream's total Length property. This is correct for a streaming method but must be understood by callers.

The filesystem assembly method creates an output stream, copies chunk files in index order, resets Position to zero, and returns it. The completion handler then hashes from that starting position and compares observed length with the session's declared TotalSize. After hashing, it resets Position again before passing the stream to blob storage. Both resets have a purpose at a different handoff.

A fake stream or fake blob port that ignores position can hide a bug. A discriminating test should consume the stream as a real reader would and verify the exact bytes seen. If the handler forgets to rewind, a normal reader begins at the end and receives no data. An assertion about SaveAsync call count alone would not detect that corruption.

## The assembled stream has an ownership lifetime

OpenAssembledAsync creates a uniquely named temporary file with DeleteOnClose and returns a read/write FileStream. It disposes that stream if assembly throws before returning. On success, the caller's await using owns disposal. The temporary assembled artifact therefore has a different lifecycle from stable chunk files and persistent blob bytes.

The completion handler's await using ensures normal method exit and exceptions dispose the assembled stream. As with other managed cleanup, abrupt process termination has different semantics from an ordinary exception path. The course does not infer universal crash cleanup from one language construct. An operational storage policy should account for what the platform and filesystem guarantee at that boundary.

The assembly method orders supplied indexes before copying. The handler already validates an exact ordered sequence, so the adapter's ordering is another local safeguard. It does not validate that the set is complete; that responsibility belongs to the handler/session contract. Passing duplicate indexes directly to the adapter would copy the corresponding file more than once unless another boundary rejects them.

## Hash agreement is about bytes, not permission

A matching final checksum establishes agreement with the caller's declared digest for the observed assembled bytes under the algorithm used. It does not establish that those bytes are safe, authorized, or of the declared MIME type. Malware scanning, access control, and content interpretation are separate responsibilities in the wider workflow.

Likewise, a content hash used as a blob lookup key is an identity convention for content. The BlobObject constructor only checks that its hash is nonblank and size is nonnegative; it does not repeat the upload session's exact SHA-256 syntax predicate. A caller constructing blobs through another path needs its own valid-content boundary. Do not transfer a guarantee from one constructor to all objects that contain a similarly named field.

The current completion handler derives the blob hash from actual assembled bytes before lookup or creation, so this path has stronger evidence than an arbitrary direct BlobObject.Create call. Source-grounded reasoning follows the path that establishes the value. A type alone may be permissive while a particular use case supplies a validated value.

## Count references without claiming content ownership

BlobObject starts RefCount at one, increments when another file version reuses the content, and decrements without going below zero through its ordinary RemoveReference method. Those methods describe local object transitions. They do not by themselves establish concurrency-safe reference accounting across database contexts or external deletion coordination.

Suppose two completions find the same existing blob and each adds a reference in a separate context. The correct persisted count depends on mapping and update behavior not proven by these methods alone. A durable implementation might use an atomic increment or concurrency token. A test using a single shared object can miss a lost-update problem that only appears with separate persisted copies.

Reference count also needs a defined meaning. Does it count file versions, active files, nondeleted versions, or another unit? The completion path increments per newly added version, but deletion and retention workflows must be examined before declaring the global invariant. The next deeper course stages will investigate those paths; this lesson teaches how to avoid premature conclusions from one side of the lifecycle.

## Build a content-evidence table

Use rows for declared total size, expected per-chunk size, observed chunk bytes, observed assembled length, computed digest, blob storage key, and reference count. Give each value a producer, a validation point, and a consumer. This table reveals when the same integer or string is being used to answer different questions.

For example, TotalSize comes from the session promise and is checked against actual assembly. StoredChunk.Size comes from reading an individual attempt. BlobObject.Size in this completion path comes from the assembled-content result. RefCount measures references rather than bytes. A dashboard that multiplies a stale reference count by blob size may produce a number, but its meaning depends on the accounting contract.

An incident report should retain these distinctions. Wrong checksum can mean a wrong declared digest, reordered chunks, changed published bytes, or a stream-position mistake. A minimal reproduction should isolate those possibilities with a tiny known byte sequence and explicit indexes. Large real files are rarely necessary to understand the causal mechanism.

## Exercise ND-06A: trace two consumers

Draw stream position before assembly returns, after hashing, before blob saving, and after disposal. Design a fake blob port that reads from the current position so a missing rewind becomes observable. Explain why call-count assertions alone cannot establish byte integrity.

## Exercise ND-06B: distinguish three lengths

Compare the session's declared size, the stream's total length, and the bytes read by hashing from a nonzero position. Give a small byte example where they differ. State which comparison the completion handler makes and why that protects the declared promise in the normal assembled-stream path.

## Exercise ND-06C: review content-addressed reuse

Explain what evidence this completion path supplies before BlobObject creation and what the constructor itself validates. Propose a separate-context reference-count test and identify the mapping or database behavior it must include. Do not claim a complete deletion or garbage-collection policy from AddReference and RemoveReference alone.

## Review checkpoint

A strong answer treats stream position and ownership as part of the interface, not incidental details. It distinguishes byte agreement from safety and authority, and local reference methods from durable accounting. Its tests consume real bytes and inspect the right state instead of relying only on successful calls.
