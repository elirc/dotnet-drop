# Review 05. Cleanup, persistence, HTTP, and storage

This review covers chapters 12 through 15. The common assessment question is whether the proposed evidence actually crosses the boundary being claimed. A database rollback, a domain exception, an HTTP response, and a filesystem artifact are different observations. Answers should connect them through an explicit sequence rather than treating them as interchangeable signs of success or failure.

## ND-12A: draw a partial sweep

With three expired sessions, the sweep changes each session in memory and deletes its chunk directory before the final SaveChanges. If deletion throws on the third session, the first two directories may already be gone, while their status changes have not reached that final save. The third session's in-memory status may also have changed before its deletion attempt throws. Do not confuse tracked state with committed state.

If all deletions succeed and the final save fails, the same broader mismatch remains: external chunk removal can precede durable expiry metadata. A database rollback cannot reconstruct deleted bytes because those filesystem operations are outside the database transaction. A later sweep may encounter already absent directories, which should be evaluated under the adapter's repeated-deletion contract.

Full credit requires a three-column trace for in-memory state, persisted state, and external artifacts. The answer should also state that the exact database exception and transaction behavior require provider evidence. The source order establishes the cross-resource concern without proving a particular deployed failure occurred.

## ND-12B: define permanent-deletion accounting

Under a retained-version logical quota policy, purging a file with two retained versions releases two version charges even if both reference one physical blob. If another file still references that blob, physical deletion is not yet eligible. Logical bytes and physical bytes intentionally differ under deduplication. A policy counting only current versions would produce different arithmetic and must be declared separately.

The inspected janitor removes old trashed nodes and saves. It does not explicitly call quota release, blob reference decrement, or blob deletion in that sweep. Cascading removal of FileVersion rows is not equivalent to those business effects. A database cascade can remove dependent rows while leaving external storage and unrelated aggregate fields unchanged.

An excellent answer names the cause identity of each release and how a repeated purge avoids applying it twice. Merely subtracting a total from UsedBytes is insufficient for replay unless the operation is guarded by durable state. The review should accept alternative accounting designs when they preserve the declared quota meaning and explain physical eligibility independently.

## ND-12C: propose safe cleanup intent

A cleanup intent can include an operation identifier, owner scope, target session or node, eligibility reason, expected state generation, discovered artifacts, and progress for each effect. Its creation should preserve the evidence needed before metadata disappears. The intent must not become blanket authority to delete any path supplied by a caller.

Completion racing with expiry requires a conditional state decision. An active writer also needs a defined ownership or lease rule so cleanup does not remove bytes still being published. Rechecking only the clock is inadequate if completion already won a state transition. The authoritative check should compare the relevant persisted state and generation, not a stale in-memory copy.

A real integration test uses a disposable database and storage root, pauses the competing actors at known gates, and verifies both final metadata and artifact outcomes. A pure model can demonstrate the intended protocol but cannot establish provider isolation or filesystem publication behavior. No destructive command belongs in the written review itself.

## ND-13A: classify model declarations

The inspected unique indexes cover user email, a file's version number, blob content hash, upload session and chunk index, share token, and direct-share node and recipient. Each prevents a particular duplicate key when enforced by the actual relational database. The file-node owner, parent, name, and deletion index is nonunique, so it is not a sibling-name uniqueness guarantee.

A unique upload-chunk key can reject two metadata rows for the same session and index. The application still needs to decide whether the losing caller receives the existing accepted result, a conflict, or another response. It also must verify that the replayed bytes match the original contract. Database uniqueness does not automatically mean that every duplicate request is semantically equivalent.

Do not claim that a unique content hash index verifies the object bytes. It enforces uniqueness of the stored hash value, not the relationship between that value and external storage. Full credit requires stating both the invariant provided and the invariant left to another layer.

## ND-13B: prove token behavior

Use two independent contexts against a disposable PostgreSQL database. Load the same entity and record its original Version in each. Mutate and save through the first context, reload independently, and observe whether the token changed. Then attempt the stale second write and record whether the provider reports a concurrency conflict and what state remains committed.

Under a proposed changing-token protocol, the second update's comparison should no longer match and must not overwrite the first accepted change silently. The current IsConcurrencyToken declaration establishes participation in a comparison, but the inspected domain Touch method does not increment Version. Therefore token generation or advancement requires additional evidence rather than assumption.

An EF InMemory fixture is useful for object behavior but does not prove the generated SQL, relational constraint enforcement, or PostgreSQL token semantics. A test report should identify its provider. Deduct credit for a concurrency test that shares one tracked entity between both actors, since it does not model independent stale reads.

## ND-13C: preserve the accounting relation

Initialize an account with capacity for only one of two proposed uploads. Coordinate both actors after reading the original usage. Assert the accepted versions, final usage, and both caller outcomes. The invariant is not merely that UsedBytes is below quota; it is that usage agrees with the accepted logical charges under the chosen policy.

An optimistic-token design can reject a stale write and retry the entire admission decision using fresh state. An atomic conditional update can reserve capacity only when the predicate still permits it. Both need a response and replay policy. Neither automatically rolls back an already published blob or removes an already written chunk directory.

The best answer includes the lost-acknowledgment case. If a charge committed but the caller did not receive success, a retry must discover the existing operation outcome rather than charging again. Protecting the quota row alone does not supply that operation identity.

## ND-14A: build an execution ladder

Domain tests can verify ExpectedChunkLength and domain guards. Validator tests can verify configured request limits. Handler tests execute handler logic but bypass controller attributes, authentication middleware, and possibly the validation pipeline if invoked directly. A hosted request fixture is needed to observe those HTTP mechanisms together.

Exact body-length enforcement requires the relevant stream-reading path, not only a DTO validator. A hosted test with fake storage can verify routing and error translation while still saying nothing about the real filesystem's pending publication. Public error shape requires middleware execution. Ownership claims require both the identity construction and the downstream predicate that uses it.

Award credit for selecting the smallest layer that proves each claim. Running the full application is not inherently stronger if the test never observes the important boundary. Conversely, a direct method call cannot prove that an unauthenticated HTTP request is rejected before reaching that method.

## ND-14B: classify absence and failure

An explicit not-found AppException supplies its selected status to the exception middleware. DomainException maps to bad request. A missing-row exception from SingleAsync is not automatically transformed into the same not-found category; absent another mapping, it follows the generic server-error path. The middleware hides detailed server exception text in the public ProblemDetails body.

A stream failure after response execution begins requires a hosted streaming observation. The inspected middleware lacks an explicit HasStarted guard, but that source fact does not justify inventing a guaranteed final status or body after headers have already been sent. State the uncertainty and design the fixture that will observe it.

The rubric rewards precise conditional language. "This direct call throws" and "this hosted request returns a particular response" are different claims. A setup failure before the server starts establishes neither public behavior nor a passing contract test.

## ND-14C: design a bounded request matrix

A valid full chunk and a valid final remainder should reach the byte-contract path with the session's expected lengths. A body that is below the transport maximum but too large for that session should still be rejected by the session-aware receiving logic. A malformed checksum should fail its applicable validation path before publication. An unauthenticated request should be stopped by the hosted authorization boundary.

For each case, name a collaborator whose absence is meaningful: no chunk publication after invalid bytes, no handler execution after authorization rejection, or no completion metadata save after a failed checksum. Record these as predictions until the selected fixture actually runs. Avoid treating the controller's broad request-size limit as the exact business length for every chunk.

## ND-15A: compare the adapter matrix

Local absent-key save creates the final file and copies bytes. Local existing-key save skips the copy. The S3-compatible path issues PutObject without the same visible skip-existing rule. A failure after a local prefix can leave an artifact requiring observation; a remote failure's aftermath depends on the service and request outcome. Repeated delete and absent open should be tested under each adapter's actual API behavior.

Existence answers whether a named artifact is present, not whether it matches the expected digest or was published through a verified protocol. A matrix that writes "success" in every existing-key cell without examining bytes misses the central issue. Remote-service configuration belongs in the evidence requirements, not in invented source guarantees.

## ND-15B: specify the interrupted-copy fixture

Use a fresh external directory and a stream that returns a known prefix before throwing. Record the final path's observable length through a read after failure. Retry with a counting stream containing the full intended bytes. If the existing path causes the second save to skip reading, the counter exposes that behavior even when the method returns a key.

The fixture must refuse an existing output directory and avoid repository storage paths. A gate makes the failure point deterministic. Compare the observed final-path behavior with the chunk adapter's pending-file publication, but do not rewrite the application to make the test pass. The initial goal is characterization.

## ND-15C: propose a storage contract revision

A verified immutable contract can publish a temporary object only after length and digest validation, then make existing-key replay return the accepted object if its identity is trustworthy. A replaceable-object contract instead defines overwrite and versioning behavior explicitly. Both must state stream ownership, cancellation aftermath, and the permitted key forms.

Thumbnail keys demonstrate why a universal hexadecimal-only key rule would break a legitimate current caller. A revised abstraction can use separate namespaces or typed keys, but migration must preserve access to existing derivatives. Verification adds reads or metadata work; immutable publication may add temporary space. Full credit requires acknowledging those costs and specifying adapter-level tests rather than promising that an interface alone enforces them.
