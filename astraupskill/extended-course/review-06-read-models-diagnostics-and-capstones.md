# Review 06. Read models, diagnostics, and capstone assessment

Complete chapters 16 through 20 before reading these answers. This final review grades whether a learner can carry evidence across multiple layers without overstating it. Capstones are proposed designs and disposable prototypes. Their success does not mean the repository now has durable reservations, version-aware processing, or a new storage protocol.

## ND-16A: predict two page traversals

With Atlas, Birch, Cedar, and Delta, the first two-item page is Atlas and Birch. Inserting Aardvark before the next offset-two request shifts Birch to that position, so it can appear again. Removing Atlas instead shifts Cedar earlier, so offset two can begin at Delta and omit Cedar from the traversal. These outcomes follow from requesting positions in two different ordered sets.

A secondary identifier key gives equal names a deterministic order. It does not freeze inserts, deletes, or renames between requests. A cursor over name and identifier avoids some offset movement but still needs a policy for items that move across the boundary. A snapshot promise requires stronger evidence than a stable tie breaker.

Full credit requires writing both page contents, not merely saying "pagination can race." The learner should explain whether duplication is acceptable for a browsing UI and whether the same behavior is acceptable for an export. Different consumers can require different consistency contracts without either being inherently wrong.

## ND-16B: define text provenance

For a current-content policy, a result should expose text only when its result version equals the current version and its processing profile is acceptable. After AddVersion, the new version is pending until its own result exists. Old extracted text can remain in historical storage but should not silently describe the new current bytes. A discriminating test searches for a phrase present only in the earlier version.

For an all-versions policy, the result needs version identity so a user can understand which historical content matched. For a last-clean-version policy, the result should explicitly name that version and indicate that a newer current version is pending or failed. Simply retaining text without exposing provenance does not establish either policy clearly.

Accept any of these choices when the DTO, transition trace, and test agree. Deduct credit if the proposed behavior is described as already implemented by the current node fields. The key skill is choosing a coherent meaning and carrying it through storage and presentation.

## ND-16C: propose an export traversal

A best-effort export can use deterministic ordering and a cursor while documenting that concurrent renames or deletions may affect inclusion. Its resume token must bind the owner scope and query policy, and callers should tolerate duplicates if the contract permits them. The export should not claim to represent one historical instant.

A snapshot export can materialize an authorized set of identifiers or use an appropriate database snapshot protocol. It must decide whether later content reads use the same version identities or merely the same nodes. Capturing node identifiers but later reading their new current versions does not produce a content snapshot. Expiry and retention of the export set also need policy.

The review should require a concrete restart story. A cursor saved after a page might be lost with the response, causing replay of that page. An idempotent downstream consumer or an export item identity can make that safe. A claim of exactly-once delivery without such a mechanism is unsupported.

## ND-17A: rename misleading metrics

Share access recording can increment an admitted-access counter at the saved RecordAccess boundary. Processing attempts increment when an attempt is actually started under the chosen protocol. Pending versions are a gauge derived from state, not a cumulative success counter. Completed transfers need a separately defined transport observation and a policy for ranges and disconnects.

Use bounded labels such as phase and outcome. File, session, and job identifiers belong in per-operation diagnostic records rather than unbounded metric dimensions. A recorded access cannot prove full delivery; an attempt count cannot prove distinct work items; a pending gauge cannot by itself identify the root cause of delay.

Full credit requires units and observation points. Names such as success_total are too vague when they mix blob publication, metadata commit, and client delivery. A metric should support a specific operational question and state the questions it cannot answer.

## ND-17B: diagnose the pending version

One hypothesis is that metadata and a job persisted but an in-memory notification disappeared during restart. Another is that a worker began and failed before producing an accepted result. Read the relevant job state, attempts, version identity, and available phase observations. The current channel's process lifetime makes the first hypothesis plausible, while worker state and error evidence can support the second.

Do not immediately enqueue arbitrary work again. First determine whether an accepted result or external derivative already exists and how duplicate execution behaves. A missing log line is not proof that no effect occurred. The next check should discriminate between hypotheses using persisted facts or a read-only artifact observation.

An excellent worksheet separates facts, interpretations, and proposed actions. It records uncertainty instead of forcing one narrative. It also avoids including file content or bearer capabilities in the diagnostic bundle merely to make the report look detailed.

## ND-17C: design a privacy-conscious event schema

A processing-start event can include work, job, node, and version identifiers, claim generation if proposed, attempt number, profile, and phase. A failure event can add a bounded category and whether the failure occurred before or after an external effect. A derivative-published event should identify the artifact and version without exposing file content or secret tokens.

Test event ordering with a recording collaborator. Pause or fail the artifact publication and assert that a confirmed-publication event has not yet appeared. Then allow success and verify its placement. This proves the vocabulary corresponds to the intended boundary in the fixture; it does not turn operational logs into durable business receipts.

The rubric should penalize logging raw passwords, share tokens, or extracted text. It should also penalize treating every missing event as an absent effect. A useful schema reduces ambiguity while respecting the limits of its delivery mechanism.

## ND-18A: write the result projection

The projection takes current version identity, available processing results, and the required profile. It selects the result whose version and profile match, or returns pending when none applies. V1 finishing after V2 cannot change the projection while V2 remains current. Restoring V1 changes the selected version and may expose its applicable result.

If the required profile changes, an older result is not automatically applicable even for unchanged bytes. The model can retain it as historical evidence while scheduling new work. The latest completion timestamp is insufficient because the latest result can belong to another version or profile. Identity determines applicability; time describes ordering.

Full credit requires a trace with both out-of-order completion and restoration. A solution that simply rejects all historical results may be valid under another policy, but it must explain how restoration obtains a result and how discarded artifacts are accounted for.

## ND-18B: reject a stale finalizer

Worker A claims generation one and pauses during external work. Its claim expires. Worker B claims generation two and finalizes an accepted result. A's later conditional finalize includes generation one and must fail because it no longer owns the work. The rejected result must not overwrite B's accepted state.

A may nevertheless have created an external artifact. Record it as unaccepted and eligible for a later reference-aware cleanup process, or use deterministic artifact identity that permits safe reuse under the contract. Do not delete blindly if another accepted result can reference the same artifact. Stale authority and artifact ownership are related but distinct questions.

The condition must be enforced where competing writes meet, such as a database conditional update. An in-memory comparison in each process before saving leaves a race. The required integration evidence includes two independent contexts and a coordinated stale-finalization attempt, not merely two sequential calls on one object.

## ND-18C: submit a capstone review packet

The packet should contain the state diagram, deterministic schedules, artifact table, migration sketch, and limitations. The six minimum scenarios are duplicate work discovery, duplicate notification after completion, process loss after publication, stale finalization, restored version, and processing-profile change. For each, assert accepted state and remaining obligations.

Grade the model on relational invariants: accepted results belong to their versions, current presentation selects applicable results, stale claims cannot finalize, and unaccepted artifacts remain accountable. Grade the migration on honesty about ambiguous existing node metadata. Reprocessing uncertain records is preferable to inventing provenance without evidence.

A prototype earns a strong grade without a real scanner or cloud service if its scope is explicit and its protocol is executable. It loses credit if it calls an in-memory dictionary durable, assumes the queue guarantees exactly-once effects, or reports unexecuted database behavior as tested.

## ND-19A: define a charge ledger

A reservation identifies temporary admitted capacity for an operation. A charge identifies retained logical usage caused by a committed version. A release refers to the charge or reservation it reverses. A physical-delete obligation identifies an external object whose reference conditions must be checked before removal. These records should not share one ambiguous Boolean status.

For success, the reservation is consumed into a charge once. For abandonment, it is released under an eligibility rule. For repeated retention, the same release identity is observed rather than subtracting again. UsedBytes is a useful aggregate, but the ledger explains its causes and supports reconciliation.

Full credit requires distinguishing logical retained bytes from deduplicated physical storage. Two versions can incur two logical charges while sharing one object. Removing one file does not authorize deletion when another live reference remains. Alternative charging policies are acceptable only when their arithmetic and user-facing meaning are explicit.

## ND-19B: resolve completion versus expiry

Both actors can initially observe Active, but only one conditional transition may succeed. If completion consumes the reservation first, cleanup must not release the committed charge. If cleanup releases first, completion must follow the chosen admission-failure or reacquisition policy rather than silently committing uncharged usage.

After a lost acknowledgment, the caller reads the operation by stable identity and observes its accepted terminal state. It does not infer failure solely from a timeout. The database transaction must connect the reservation transition and the corresponding charge or release records. External blob publication remains a separate recovery concern.

A process-local lock demonstrates only one process's serialization. Cross-process enforcement requires a shared authority and provider-backed evidence. The rubric should explicitly reject a test that calls two methods under the same lock and then labels the result distributed-safe.

## ND-19C: build a dry-run reconciler

The report should classify each discrepancy without immediately repairing it. A version lacking a charge is a missing-charge candidate. Two charges with the same cause indicate duplicate accounting. An aggregate disagreement requires comparison with the ledger's declared states. A live reference to a missing object is an availability concern, while an unreferenced object is a potential cleanup candidate.

For each category, include the identifiers needed for a read-only follow-up, the policy assumption, and the proposed next check. Avoid printing file contents. Ambiguous records should remain ambiguous until evidence resolves them. A dry-run tool is safer and more reviewable than a repair routine that guesses which side of a mismatch is authoritative.

The strongest rubric requires deterministic output on the synthetic dataset, no writes, bounded arithmetic, and an explanation of false-positive possibilities such as delayed operations. A clean report on five synthetic rows proves classification logic for those rows, not completeness over a production inventory.

## ND-20A: draw the ownership ledger

The request body belongs to the hosting request lifecycle unless a component explicitly takes ownership. A service-opened blob stream transfers an obligation to its consumer. In a direct FileService test, the test must dispose the returned stream; in a hosted download, the File result participates in the framework's response lifecycle. These are different execution contexts.

The worker's sample and image streams are independently opened resources. Their using scopes should be inspected separately. A tracking stream can prove reads and disposal in a focused fixture. Do not assume disposing one stream closes another simply because both refer to the same blob key.

An answer may mark an ownership question unresolved when the inspected abstraction does not document it. That is better than inventing a contract. The proposed interface documentation should state whether input streams are borrowed and whether returned streams transfer disposal responsibility.

## ND-20B: choose a shutdown policy

Bounded bookkeeping after cancellation can use a separate short-lived token to attempt recording an outcome, provided the host allows the grace period and the failure remains recoverable. Recoverable claims instead allow interrupted work to remain running until a recovery process reclaims it. Both require explicit limits and duplicate-effect handling.

Reusing the already canceled token for SaveChanges cannot prove that the failure record will persist. The save may itself observe cancellation. A robust report records the original interruption and the attempted bookkeeping separately. It does not treat the catch block's presence as evidence that recovery always succeeds.

Grade the chosen policy against its prerequisites. A claim-recovery proposal without a persisted claim and recovery query is incomplete. An unlimited shutdown delay is also incomplete because it has no bounded operational behavior. A small deterministic model can compare the policies before provider integration.

## ND-20C: inject a cleanup failure

Have the copy collaborator record a prefix effect and throw an identified primary failure. Have cleanup record its attempt and throw a second failure. The result or diagnostic record should preserve both identities and the remaining artifact obligation. It should not replace the original copy cause with an unrelated cleanup message.

On retry, use the cleanup operation's identity to avoid repeating a logical release. Physical deletion can treat already absent as terminal success under its contract, while quota release must be tied to the original charge. This demonstrates why one generic retry loop is insufficient for different resource types.

The final assessment asks the learner to explain a complete operation from admission through bytes, metadata, processing, download, and retention. A passing submission identifies boundaries and tests them. A strong submission also explains what remains unproven and proposes the smallest next experiment. That evidence discipline is the course's transferable outcome.
