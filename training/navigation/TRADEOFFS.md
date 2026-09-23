# Senior trade-offs in NetDropbox — "Why?" drills

For each decision, fill in the missing parts yourself first. The reference points are given in brief; argue with them.

## 1. Content-addressed blobs with dedupe and refcounts (`CompleteUpload.cs:51-61`, `BlobObject.cs`)
- **Problem:** users store identical bytes, and storage costs money.
- **Naive approach:** one object per upload.
- **Where it fails:** there's duplicate storage, and "copy" has to copy bytes.
- **Options:** A) per-upload objects; B) content-addressed plus refcount in the DB; C) content-addressed plus GC by mark-and-sweep (no refcount).
- **Trade-offs:** B makes copy O(1) and saves storage, but refcount drift and a GC race appear (M4); C is simpler and always correct, but its sweep cost grows with the data.
- **Choice:** B.
- **Failure modes:** refcounts that only ever increase (nothing decrements them today); a deleted-while-referenced race; a one-user infected blob affecting all users; a hash collision (negligible with SHA-256).
- **How it's tested:** not at all today.
- **How it's monitored:** not at all today.
- **When to revisit:** when GC correctness incidents appear, or when you move to presigned multipart uploads, where the server no longer sees the bytes before the key is chosen (S3).

## 2. An in-process `Channel` as the job queue (`ChannelProcessingQueue.cs`)
- **Problem:** scanning must not block `complete`.
- **Naive approach:** scan inline.
- **Where it fails:** timeouts on large files; upload availability tied to the scanner.
- **Options:** A) an in-memory Channel; B) a DB jobs table with `SKIP LOCKED`; C) an outbox plus a broker (RabbitMQ is sitting in compose).
- **Trade-offs:** A has zero ops but loses work on restart and works on a single node only; B is durable and transactional with modest throughput; C scales and fans out, but adds a system and a relay.
- **Choice:** A. It was probably chosen for demo simplicity, while `processing_jobs` hints that B was intended.
- **Failure modes:** lost messages, and PendingScan forever (review #6).
- **Tested / monitored:** neither.
- **When to revisit:** as soon as there's more than one replica, or deploys happen during business hours. That's now. See S1.

## 3. Scan status on `FileNode` (`FileNode.cs:25`)
- **Problem:** the UI and the download gate need "is this file safe?".
- **Naive approach:** a status column on the thing the user sees.
- **Where it fails:** versions, copies and dedupe all mean the bytes and the node diverge (review #3–#4).
- **Options:** status per node, per version, or per blob.
- **Trade-offs:** per node makes queries simple but the invariant wrong; per version is correct but rescans deduped content; per blob is correct and efficient but needs a join on every gate check.
- **Choice:** per node.
- **When to revisit:** now (M3/S2).

## 4. Chunked, resumable uploads through the API with server-side assembly (`UploadChunk.cs`, `FileSystemChunkStorage`)
- **Problem:** large files, flaky networks.
- **Options:** A) a single PUT; B) chunk through the API to local disk; C) presigned S3 multipart.
- **Trade-offs:** B gives the server full control (per-chunk checksum, quota, dedupe before storing), but costs API bandwidth, stickiness to one replica, and 2× disk use during assembly. C scales, but the server sees the bytes only after the upload.
- **Choice:** B.
- **Failure modes:** multi-replica breakage (S3), unverified overwrite of a chunk (review #15), 2× disk for the temp assembly.
- **When to revisit:** at a second replica, or when you pay for egress through the API.

## 5. MediatR for uploads, plain services for files and shares (`Program.cs:28-30`)
- **Problem:** where cross-cutting concerns (validation, logging) live.
- **Options:** everything MediatR; everything services; a mix.
- **Trade-offs:** MediatR gives uniform pipeline behaviors but indirection and "where is the handler?" friction; services are direct but need validation repeated by hand. A mix gives both costs and a split mental model.
- **Choice:** a mix. It's probably not a decision at all, which is a legitimate finding.
- **When to revisit:** the next time a cross-cutting concern such as authorization or auditing must apply everywhere (INC-003). Pick one style then.
