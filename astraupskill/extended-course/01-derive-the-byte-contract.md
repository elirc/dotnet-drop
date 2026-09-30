# 01. Derive the byte contract before moving data

An upload session describes a promise about a sequence of bytes. It names the total size, a final SHA-256 value, and a chunk size. The implementation must turn that promise into exact per-index boundaries before accepting data. This chapter derives the arithmetic from current source and uses it to build a prediction table. It is the first step of a usable path through chunk acceptance, completion, recovery design, and a capstone.

Read [UploadSession](../../src/NetDropbox.Domain/UploadSession.cs) and [the domain tests](../../tests/NetDropbox.Tests/Domain/UploadSessionTests.cs). You need integer division, zero-based indexes, and the difference between int and long. You do not need a database or a running API. Write down the units beside every variable: totalSize and expected size are bytes, chunkSize is bytes per chunk, and index and totalChunks are counts.

## Derive the positive-size formula

For positive total size, the source computes requiredChunks as one plus the integer quotient of totalSize minus one divided by chunkSize. This is a ceiling division arranged to avoid adding chunkSize minus one to a potentially large total. The commonly memorized addition formula can overflow before division even when the final mathematical answer would fit.

Take ten bytes with a chunk size of four. The quotient of nine divided by four is two, so three chunks are required. Their indexes are zero, one, and two. The first two chunks contain four bytes each; the last contains two. The sum is ten, and every index has a single expected length. That makes a retry comparable to a fixed boundary rather than an arbitrary segment of data.

For eight bytes with size four, the same formula gives two chunks, not three. Exact multiples are an important counterexample because a careless floor-plus-one implementation creates an extra empty chunk. For one byte with size four, one chunk is required and its expected length is one. These tiny fixtures are easier to reason about than a large file whose remainder is overlooked.

## Zero bytes is a deliberate special case

The source defines one chunk for an empty upload and returns expected size zero for that chunk. This is a protocol choice. It means completion still expects the index set containing zero, even though the accepted body is empty. A client that assumes empty files have no chunk records would disagree with this session contract.

Do not replace the special case with a preferred convention while writing tests. First characterize the actual behavior, then propose a protocol change separately if there is a reason. Empty-file handling touches session creation, chunk acceptance, checksum values, and completion's required index set. Changing only totalChunks can leave the workflow inconsistent.

The final hash must still be a valid sixty-four-character hexadecimal string. A syntactically valid hash does not prove it matches the content; that is checked during completion. For an empty file, a caller needs the digest of the empty byte sequence, not a missing value or a row of arbitrary hexadecimal characters. Syntax and content verification are different stages.

## Check the count before narrowing

The required chunk count is computed using long-sized arithmetic and rejected if it exceeds int.MaxValue. Only then is it converted to int. This preserves a meaningful error rather than silently wrapping a huge count into a negative or small value. The source also rejects negative total size and nonpositive chunk size before using the formula.

The expected-size method validates the index against TotalChunks, computes the offset using a long multiplication, and returns the smaller of chunkSize and remaining bytes. The cast before multiplication matters. Multiplying two ints first and then converting the result would permit overflow before the wider representation is used.

These are separate protections: valid input ranges, overflow-safe ceiling division, count capacity, index range, and wide offset multiplication. A single test named validates size is too vague to establish all of them. Build a boundary matrix that identifies which protection each fixture exercises and what should remain unchanged after rejection.

## Normalization happens after some validation

Create checks the final hash with an exact length and hexadecimal predicate. The private constructor then trims and lowercases it. Because validation happens first, surrounding whitespace makes the supplied hash too long and is rejected rather than accepted through trimming. This is a useful reminder that the existence of a normalization line does not determine which raw inputs reach it.

The file name is checked for blankness and trimmed. MimeType falls back to application/octet-stream when blank, but otherwise is stored as supplied in this constructor. These observations describe this boundary only; they do not establish a complete filename safety policy or content-type verification. A declared MIME value is not evidence that the bytes have that format.

OwnerId must be nonempty, and ParentId can be nullable. The session stores ownership, but authenticated authority is established elsewhere in a full request path. A domain object accepting an owner identifier cannot prove that the caller is entitled to use it. Later lessons will trace that distinction rather than assigning every security responsibility to this constructor.

## Time and lifecycle are another boundary

The constructor sets expiry to twelve hours after the current system UTC time. IsExpired returns true when ExpiresAt is at or before the supplied time, or when status is explicitly Expired. The method therefore includes the exact equality boundary. Completion and chunk handlers use their injected clock to ask whether the session is expired.

Complete itself calls EnsureOpen and validates the completed file identifier, but it does not independently compare the current time with ExpiresAt. The handler performs that time check before proceeding. A direct domain call and a complete application workflow therefore have different preconditions. Tests should name which layer they exercise instead of assuming every guard appears everywhere.

Expiry creation currently uses system time inside the domain constructor, while handler checks use an injected clock. That is a concrete design tension for deterministic time tests. A proposed clock refactor should identify which values are captured and preserve existing boundaries; it should not claim all time behavior is already controlled by the handler's clock.

## Build a prediction ledger

Use total/chunk pairs of zero/four, one/four, eight/four, and ten/four. For each, list every valid index and expected size. Then add one negative index and an index equal to TotalChunks. Finally add a very large total with a tiny chunk size to exercise the count-capacity rejection. Keep mathematical reasoning separate from actual execution in your notebook.

For every accepted session, verify two invariants: the expected sizes sum to TotalSize, and each nonfinal positive-size chunk has the configured chunk size. The final chunk is positive for a positive file and can be smaller. The zero-file special case has one zero-size chunk. Those relationships provide an independent oracle for the formula.

## Exercise ND-01A: derive four partitions

Write the complete index and size tables for the four small examples. Explain why the exact-multiple case does not create an extra chunk and why the empty-file case does. Identify which variables require long arithmetic and which values are narrowed only after a capacity check.

## Exercise ND-01B: distinguish syntax from truth

Compare a valid uppercase hash, a valid hash with surrounding spaces, sixty-four nonhexadecimal characters, and a syntactically valid hash for the wrong content. State which stage can reject each input. Explain why a declared MIME type and a valid hash string are not proof of content safety.

## Exercise ND-01C: map time ownership

Describe expiry creation, IsExpired equality, handler checks, and Complete's own guards. Propose a deterministic time test without pretending that the existing constructor accepts an injected clock. Identify a minimal future API change that could make time ownership more consistent, but leave application source unchanged.

## Review checkpoint

A strong answer derives the byte partition, identifies the order of validation and normalization, and distinguishes domain lifecycle from handler preconditions. It can explain every boundary using a small counterexample. The goal is not to memorize one ceiling-division formula, but to make the upload's byte promise explicit before any external work begins.
