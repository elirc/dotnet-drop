# 02. Publication is not the same as receiving

A server can receive bytes without accepting them as a chunk. Netdrop's filesystem adapter writes incoming data to a uniquely named pending file, validates length and checksum, and only then attempts to publish the stable chunk path. This chapter follows that distinction through identical retries and conflicting concurrent writers. The objective is to identify what becomes authoritative at each stage.

Read [FileSystemChunkStorage](../../src/NetDropbox.Infrastructure/Storage/FileSystemChunkStorage.cs), [UploadChunkHandler](../../src/NetDropbox.Application/Uploads/UploadChunk.cs), and [filesystem tests](../../tests/NetDropbox.Tests/Uploads/FileSystemChunkStorageTests.cs). The adapter owns actual file publication. The handler owns session checks, expected size, and database chunk metadata. Neither boundary alone represents the entire upload workflow.

## The pending file has no acceptance authority

StoreAsync derives a stable chunk path from session Id and index, creates its parent directory, and creates a pending path with a random suffix. Incoming data is read into a bounded buffer and written there while a SHA-256 calculation advances. A pending file is temporary working state, not proof that a valid chunk exists at the stable path.

The read loop limits how much it asks from the input. Near the expected boundary, it allows one extra byte so oversized content can be detected. If the observed total exceeds expected size, the loop stops and acceptance remains false. It does not need to consume an arbitrarily large body merely to discover that it is too long. This bounds additional read work after the declared limit, although the surrounding transport has its own responsibilities.

The oversized byte count and checksum have different scopes. The code can record a total that includes the excess read while hashing only bytes written before the overflow decision. That is sufficient for rejection because both exact size and expected checksum are required for acceptance. Do not treat a rejected StoredChunk checksum as a digest of every byte the client intended to send.

## Publication requires two agreements

After reading, accepted is true only when observed size equals expected size and checksum matches case-insensitively. A body with the right size but wrong bytes fails. A body with the right prefix but extra bytes fails. A truncated body fails even if its supplied checksum happens to describe that truncated body, because the session's expected size remains authoritative.

Only accepted content reaches File.Move from pending path to stable chunk path. The move does not request overwrite. This is important under concurrency: two first writers can both finish validation, but they must not silently replace one another's accepted chunk. The stable path becomes a shared publication boundary for that session/index pair.

If a move throws IOException and the stable path exists, the adapter inspects the published file. Matching size and checksum make an identical retry acceptable. Different content causes ChunkWriteConflictException. If the IOException occurred for another reason and there is no published path, the catch filter does not turn it into a successful retry. Error classification follows an observed condition rather than swallowing every filesystem failure.

## Work two overlapping writers

Writer A and writer B target the same session and index. They have separate pending files. Both bodies pass their own expected-size and checksum checks. A publishes first. When B tries to publish, the stable path already exists. If B describes the same bytes, inspection agrees and B returns an accepted outcome without replacing A's file. Its leftover pending file is removed in finally.

If B describes different bytes, inspection disagrees and the adapter throws a conflict. The accepted file remains A's. The conflict does not mean B failed to transmit bytes; it means those bytes did not become the accepted chunk for that identity. This is why a progress UI should distinguish sent, validated, and accepted states rather than treating a completed network write as final acceptance.

The exact winner depends on scheduling. A test should not require writer A to win merely because its task was created first. It should require one stable accepted content and reject an incompatible attempt. For identical concurrent writers, both can report acceptance of the same published content. The behavior is not one successful response and one failure in every race.

## The handler adds a metadata boundary

UploadChunkHandler first finds the session by Id and OwnerId, checks open/expiry state and index range, and looks for existing database metadata. If metadata exists with a different checksum, it rejects before calling storage. This early guard avoids unnecessary work for a known conflict. It does not eliminate the need for filesystem conflict protection because two first requests can both observe no metadata.

For an identical existing metadata row, the handler still calls StoreAsync with the submitted stream and expected length. A matching checksum string alone is not enough to skip verification of the new body. If storage returns Committed false, the handler chooses a size or checksum error. If storage reports acceptance and no metadata existed, it adds the row and saves changes.

That sequence creates a possible file-before-metadata window. Publication can succeed before database saving fails. The course does not claim an atomic transaction spans filesystem and database. An eventual retry may encounter the published bytes without a metadata row. A robust recovery design needs to reason about both states rather than assuming one always implies the other.

## Cleanup is scoped but not magic

The finally block deletes the attempt's pending file if it still exists. A successful move means that path no longer exists; an identical retry leaves its pending file to be cleaned. This cleanup concerns a specific attempt-owned path. It does not remove the stable accepted file after a conflict, which would destroy the winning writer's result.

Process termination can prevent managed finally code from running. Therefore the presence of finally is not proof that no abandoned pending file can ever remain. A proposed housekeeping job could remove stale attempt files under an owned storage root, but it would need safe age, naming, and concurrency rules. That proposal is separate from the current synchronous cleanup path.

Cancellation is passed to stream reads, writes, and inspection hashing. A cancellation exception still passes through finally. It does not imply that earlier reads or writes never occurred. If publication has already succeeded, later caller abandonment is not automatically a rollback request. Characterize the actual observation points before promising cancellation semantics to a client.

## Exercise ND-02A: draw the publication ledger

For a valid chunk, a truncated chunk, an oversized chunk, and a checksum mismatch, list pending-file existence, observed bytes, acceptance decision, stable-path publication, and cleanup. Explain why a rejected checksum value need not describe the entire original stream. Keep protocol state separate from transport progress.

## Exercise ND-02B: compare identical and conflicting races

Create two-writer schedules for identical and different bodies at one session/index. State permitted responses and final stable-file contents without assuming which task wins. Identify the filesystem operation that prevents silent overwrite and the inspection that distinguishes replay from conflict.

## Exercise ND-02C: investigate an orphaned publication

Assume storage publication succeeds and database SaveChanges fails. Write a state table for files and metadata, then propose a retry or reconciliation experiment. Explain which parts can be tested with temporary files and which require database-specific evidence. Do not claim the existing handler provides a distributed transaction.

## Review checkpoint

A complete answer distinguishes received bytes, validated pending content, accepted publication, and saved metadata. It understands why both handler and adapter guards exist, why identical races can both succeed, and why cleanup code does not erase crash-recovery questions. Those distinctions are the basis for the completion incident in the next chapter.
