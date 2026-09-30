# 05. Resume identity and expired open sessions

Resuming an upload is an identity problem before it is a transfer problem. A start request can reuse an existing session, but the matching fields determine which earlier promise is recovered. This chapter follows StartUploadHandler and compares its lookup with UploadSession's stored normalization and later expiry checks. The goal is to identify a precise resume contract rather than assume that every request with a similar filename describes the same upload.

Read [StartUpload](../../src/NetDropbox.Application/Uploads/StartUpload.cs), [UploadSession](../../src/NetDropbox.Domain/UploadSession.cs), and [UploadChunk](../../src/NetDropbox.Application/Uploads/UploadChunk.cs). StartUploadHandler first loads the user, performs a quota comparison, checks a supplied parent folder, and then searches for an open session. It does not reserve quota when the session is started. The later completion path performs its own preflight and reservation.

## Derive the current lookup identity

The existing-session query compares OwnerId, ParentId, FileName, FinalContentHash, and Open status, then chooses the newest CreatedAt. It does not compare TotalSize, ChunkSize, or MimeType. It also does not include an expiry predicate. Those omissions are observations of the current query; deciding whether to change them requires a resume policy.

Suppose a client starts a file with chunk size one megabyte, then sends another start request for the same owner, parent, name, and hash with chunk size two megabytes. The handler can return the existing session and its original ChunkSize and TotalChunks. The response, not the latest request's preferred chunk size, describes the recovered session's byte boundaries. A client that ignores the response and uploads using its new preference can violate the session contract.

Total size has a similar role. A matching session can retain its original declared size even when the new start request supplies another value. Since the final hash is meant to identify content, inconsistent size may indicate an erroneous client or a changed contract. The current query does not resolve that inconsistency by comparing every field. A proposed design should explicitly choose reuse-with-original-values, conflict, or a new session.

## Normalization and lookup can disagree

UploadSession stores a trimmed filename and lowercase final hash. The lookup uses raw request fields in equality predicates. An uppercase hash that passes domain syntax validation is normalized when a new session is created, but a later raw uppercase request may not match the stored lowercase string under a case-sensitive provider comparison. Exact behavior can depend on provider collation, so do not claim a universal database outcome from the expression alone.

Filename whitespace creates a related distinction. A newly stored filename is trimmed. A later request carrying surrounding spaces may compare differently before any new domain object is constructed. The existence of normalization in the constructor does not normalize the existing-session query automatically. Normalization belongs in a deliberate identity boundary if reuse depends on it.

The start validator constrains filename length, nonnegative total size, hash length, and a chunk-size interval. The domain constructor adds other checks, including hexadecimal hash syntax. If an existing session is reused, the constructor is not called for that request. A complete validation analysis must therefore distinguish request-pipeline validation from guards that only run during new object construction. Direct handler tests do not automatically execute the mediator's validation pipeline.

## Open and unexpired are different facts

An UploadSession can have Status Open while its ExpiresAt is already in the past. IsExpired checks both time and explicit Expired status, but StartUploadHandler's reuse query only checks status. It can therefore select an open-but-time-expired session under the inspected control flow. Later chunk and completion handlers call IsExpired and reject it.

This is a useful incident hypothesis: a client successfully obtains a session response from start but cannot upload a chunk because the recovered session is expired. The source supports the path, but a reported production incident would still need a reproducing fixture and the actual request configuration. Keep predicted behavior and executed evidence separate.

A proposed fix might exclude expired sessions from reuse and create a new session, or explicitly return an expiry conflict requiring a fresh start. The choice affects cleanup and client state. Creating another session without a policy can leave abandoned chunk files and confusing resume choices. Rejecting without a recovery path can leave clients stuck. Specify the desired behavior before moving the predicate.

## Parent ownership is checked at start

When ParentId is supplied, the handler requires a nondeleted folder owned by the requesting user. This is a concrete parent-scope guard. It does not prove that the request's UserId was authenticated, because the handler receives an identifier from its caller. A hosted endpoint and identity mapping must establish that upstream authority.

The parent can change after session creation. A future completion policy must decide what happens if the folder is deleted, moved, or loses visibility while an upload is in progress. The start-time check is evidence about one observation, not a permanent guarantee about the destination. The current completion path uses session.ParentId in its file lookup and creation; a stronger lifecycle policy needs explicit investigation.

This makes a good cross-stage consistency exercise. Record folder state at start, session state during transfer, and destination state at completion. Do not assume that a successful start freezes the folder or user quota. A long-running upload spans time during which other operations can change relevant state.

## Quota at start is an early observation

StartUploadHandler compares UsedBytes plus requested TotalSize with QuotaBytes. Completion uses the domain's read-only EnsureCanReserve and later Reserve. These are different implementations of related policy. The addition form deserves numerical-boundary review, while the domain method uses subtraction after checking an overquota state. The exact overflow outcome depends on the compiled arithmetic context; this chapter does not claim a hosted response without executing it.

Even a numerically safe start check does not reserve capacity. Another upload can consume quota before completion. A client should treat session creation as permission to attempt transfer under the protocol, not a promise that capacity can never change. If the product wants reserved upload capacity, that is a new state and lifecycle requirement with expiry and release rules.

## Exercise ND-05A: define the resume key

List every field in the current existing-session query and every supplied field it omits. Create requests that differ only in chunk size, total size, or MIME type. Predict which session values a reused response returns. State what the client must use when deciding chunk boundaries after resume.

## Exercise ND-05B: reproduce expired-open reuse on paper

Build a fixture with an Open session whose ExpiresAt is before the handler clock used by later commands. Trace start lookup, returned session, and subsequent chunk rejection. Propose two product policies and compare their cleanup and client-recovery consequences. Do not label either policy implemented.

## Exercise ND-05C: normalize at the correct boundary

Design a request-to-resume identity function for filename and hash. Explain provider-sensitive comparisons, raw versus stored values, and the validation path when a session is reused. Include parent changes and quota changes as separate consistency questions rather than hiding them inside string normalization.

## Review checkpoint

A strong answer treats resume as recovery of an existing byte promise, not a chance to silently revise it. It identifies the difference between Open and unexpired, between constructor normalization and query comparison, and between a start-time check and a durable reservation. Those distinctions make client behavior and future fixes reviewable.
