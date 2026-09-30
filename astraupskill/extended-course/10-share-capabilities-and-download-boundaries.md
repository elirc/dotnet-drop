# 10. Share capabilities and download boundaries

A share link is a capability-like value: possession of a token can grant access under additional rules. Netdrop also supports direct shares tied to user identities. This chapter compares their actual service paths with owner downloads, focusing on expiration, revocation, permission, deletion state, and access-count meaning. Use synthetic tokens and test data only; no real credentials or files are needed to reason about these boundaries.

Read [ShareService](../../src/NetDropbox.Application/Shares/ShareService.cs), [ShareLink](../../src/NetDropbox.Domain/ShareLink.cs), [TokenGenerator](../../src/NetDropbox.Application/Shares/TokenGenerator.cs), and [FileService](../../src/NetDropbox.Application/Files/FileService.cs). The token generator fills thirty-two random bytes and encodes them in a URL-safe Base64 form without padding. That is a concrete generation mechanism, not proof that every surrounding access policy is correct.

## Creation and use have different checks

CreateLinkAsync requires an owned nondeleted node, generates a token, optionally hashes a nonblank password, creates a link, and saves. It returns token and metadata. The domain constructor requires a nonblank token but does not independently validate node existence, permission range, or future expiry. Those facts are supplied or checked by other boundaries if required.

OpenPublicDownloadAsync finds the token, checks IsUsable with system UTC time, verifies a password when one is stored, and requires permission at least Download. IsUsable rejects revoked links and treats expiry equality as unusable because ExpiresAt must be strictly greater than now. A link with no expiry can remain usable until revoked under this method.

Permission comparison follows enum numeric ordering: View is below Download, and EditUpload is above it. Therefore EditUpload also passes the download threshold. That can be intentional, but it should be documented as an ordering-based policy rather than assumed from the names. Undefined numeric enum values require a separate input-validation review before making a complete HTTP claim.

## Owner and public download paths differ

Owner OpenDownloadAsync requires a matching owner and a nondeleted node, checks file type, and rejects pending, infected, or failed scan status. It can choose a supplied historical version or the current version. Public download loads the node by the link's node Id, checks that ScanStatus is Clean, and selects the current version. Its query does not include the owner's nondeleted predicate, and this method does not explicitly check File type.

Those differences are consequential source observations. A soft-deleted clean node can remain eligible under the public method's visible predicates if its link is still usable and related records remain present. A folder link can reach a current-version lookup that has no normal file version. A complete test should exercise the actual service with a controlled context and blob port before assigning exact hosted responses.

Do not fix the discrepancy in this learning task. Write a guard matrix and a proposed policy. If deletion is supposed to revoke public availability, that condition should be checked or enforced consistently at the download authority. If historical links intentionally survive soft deletion, the product needs an explicit rule and user-facing explanation. Ambiguity is the issue to resolve first.

## Access count is recorded before opening bytes

After resolving the current version and blob, public download calls RecordAccess and saves, then opens the blob stream. If blob opening fails, the access count can already be incremented. The metric therefore represents passage through an authorization and metadata stage, not necessarily a completed download or even successful stream opening.

This matters for analytics and billing. A dashboard labeled completed downloads would overstate what AccessCount establishes. A more precise label might describe authorized download attempts under the current sequence. If the product needs completed transfers, it requires a different observation point, possibly involving response execution and client behavior, with its own reliability limits.

Concurrent access updates can also compete. ShareLink's increment is a local long addition, and the inspected model does not declare a concurrency token for that entity. Separate-context persistence behavior needs testing before claiming exact counts under load. A counter useful for approximate analytics may need a different implementation if it becomes a security or billing authority.

## Revocation has an observation point

RevokeLinkAsync finds the link, verifies ownership through the associated node, marks it revoked, and saves. A download that checked usability before that save may already be progressing. Revocation does not automatically cancel an already-open stream under the inspected methods. Define whether the product promises to block new admissions or terminate active transfers before choosing a test.

An admission-only policy can be reasonable and testable: requests whose usability check occurs after revocation must fail, while already admitted streams may finish. A stronger active-transfer policy needs shared cancellation or lease infrastructure and clear behavior for partial responses. Merely checking a flag once cannot establish it.

Use a gate-based service fixture to order the usability check and revocation. Avoid arbitrary sleeps that make it unclear whether the request was admitted before or after the state change. The result should identify the policy's linearization point and the boundary it actually exercises.

## Direct sharing is another protocol

DirectShareAsync verifies an owned nondeleted node, finds a target user by normalized email, and adds a role record. The database model has a unique node/recipient index. Repeating the same share is not handled as an explicit application-level update in this method; the provider's uniqueness behavior may become observable. A proposed idempotent share command would need a defined role-change policy.

SharedWithMeAsync lists nodes joined from recipient shares and filters deleted nodes. That listing does not itself prove that the recipient can use every owner-only FileService operation on the listed node. Read authorization at each operation. A UI showing a shared item and a download service requiring OwnerId can be inconsistent unless another route or authorization path connects them.

The Role enum expresses Viewer and Editor, but this service code does not by itself demonstrate all operations permitted by those roles. A complete capability matrix needs endpoint and application evidence. Do not infer editing authority solely because a stored field is named Editor.

## Exercise ND-10A: compare download guards

Build a table for owner and public downloads covering owner scope, deletion, file type, scan state, version selection, password, permission, and expiry. Use the actual predicates. Propose a consistent policy for soft-deleted files and identify which current path would need change.

## Exercise ND-10B: define an access metric

Trace a public request whose metadata checks pass but blob opening fails. State the resulting count meaning and propose names for attempt versus completed-transfer metrics. Include a concurrency limitation and explain why one local increment does not prove exact global accounting.

## Exercise ND-10C: specify revocation semantics

Choose admission-only or active-transfer revocation. Write a controlled two-actor schedule and the expected result before and after the relevant observation point. Identify the additional mechanism required for the stronger policy. Keep the proposal separate from current one-time usability checking.

## Review checkpoint

A strong answer treats tokens, permissions, ownership, and lifecycle as distinct checks. It can explain a successful metadata admission without claiming completed transfer, and it compares public and owner paths instead of assuming they inherit one another's rules. Its proposed security contract is precise enough to test without using real user data.
