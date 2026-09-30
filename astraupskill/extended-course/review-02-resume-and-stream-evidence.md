# Review 02: resume and stream evidence

Try chapters 05-06 before reading these answers. The central question is which earlier promise a later operation relies on. A resumed session keeps its original boundaries. A hashing method observes bytes from the current stream position. A blob object stores values supplied by its caller. Understanding those handoffs prevents accidental substitution of a fresh request preference for authoritative existing state.

## ND-05A: reuse recovers the original session

The query matches owner, parent, filename, final hash, and Open status, then chooses the newest creation time. It omits total size, chunk size, MIME type, and time expiry. When it reuses a session, the response contains that session's original chunk size and total chunk count. The client must use those returned boundaries rather than assuming its new preferred chunk size took effect.

A changed total size under the same matching fields deserves an explicit product policy. Current lookup does not compare it. A proposed design could reject the inconsistency, return the original promise clearly, or require a new identity. Silently changing the existing session while retaining accepted chunks would require a much more complex compatibility analysis.

The answer should not claim that every start request constructs a new UploadSession. The existing-session branch bypasses that constructor, so constructor-only guards and normalization do not automatically execute for reused sessions. Request validation and mediator pipeline behavior remain separate evidence.

## ND-05B: status and clock can disagree

Create an Open session and choose a later handler clock beyond ExpiresAt. The start query can select it because it filters only Open status. A later chunk handler calls IsExpired with its clock and rejects the session as no longer open for operations. The source supports this predicted path; a real incident claim still needs a reproducing test under the configured provider and pipeline.

One proposed policy excludes expired sessions from reuse and starts a new one. Another returns an explicit expired-session response with instructions to restart. Both need cleanup rules for abandoned chunks and a client state transition. Neither is already supplied by changing the word open in documentation.

The strongest answer explains how a client clears or replaces its stored session identity and avoids uploading with stale boundaries. It also avoids treating a new session as a quota reservation, because the current start path does not reserve capacity.

## ND-05C: normalize before identity comparison

A proposed identity function can trim filename and normalize hash case before both lookup and new object creation. Its exact policy should match accepted raw input and database comparison semantics. If surrounding hash whitespace is to remain invalid, validation must precede or accompany normalization deliberately rather than accidentally broadening the accepted input language.

Provider collation matters for raw equality in the current query. A pure string comparison model cannot establish the actual database's case behavior. The design should test normalized values and query translation at the integration layer when making that claim.

Parent and quota changes are not string problems. A successful start observes one folder and account state. Later deletion, movement, or capacity consumption requires a lifecycle policy at completion. A complete answer keeps those consistency questions separate from resume-key normalization.

## ND-06A: rewind is observable behavior

Assembly returns the stream at position zero. Hashing reads it to the end. Completion resets Position to zero before blob saving. Disposal occurs when the handler leaves its await-using scope. A fake blob port should actually read the stream and compare bytes with the expected fixture, so removing the reset causes a clear failure.

Counting one SaveAsync invocation cannot prove the right content was saved. A fake that ignores its stream argument would allow an empty-at-end stream to pass. The test should assert both call ordering and bytes observed where those are part of the intended contract.

## ND-06B: observed length is relative to the read

For a four-byte stream positioned at two, hashing observes the final two bytes and returns length two. The stream's total Length can remain four, and a session may declare four. The hashing method does not promise to inspect from zero or rewind itself. The caller must establish the intended starting position.

The normal assembly adapter resets to zero before returning, and completion compares hashing's observed length with session.TotalSize. That path checks actual bytes read rather than trusting metadata alone. A direct call to Hashing on a partially consumed stream has a different precondition and should be labeled accordingly.

## ND-06C: follow validated values through the path

Completion computes the content hash and observed size before creating BlobObject, giving this path actual byte evidence. BlobObject.Create itself only rejects blank hash and negative size. It does not validate sixty-four hexadecimal characters or inspect a stream. Another caller cannot claim the upload path's guarantees merely by using the same type.

A reference-count integration test needs separate contexts reading the same blob and competing updates under the actual provider and mapping. A shared in-memory object test proves only local increments. The complete lifecycle also needs deletion and retention paths before RefCount can be treated as an authoritative global ownership measure.

## Rubric

Assess whether each value's producer and consumer are named. Strong answers preserve original session boundaries, identify provider-dependent comparisons, and test bytes rather than call counts. They recognize that a good local guard can coexist with an unresolved cross-stage policy. They propose targeted next evidence instead of replacing every uncertainty with a broad rewrite.
