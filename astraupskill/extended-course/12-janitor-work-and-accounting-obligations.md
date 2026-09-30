# 12. Janitor work and accounting obligations

Background cleanup is a state-changing workflow, not merely a collection of delete calls. Netdrop's JanitorWorker expires old upload sessions, deletes their chunk directories, and removes sufficiently old trash nodes. This chapter examines the order of those effects and the accounting obligations that are not visible in that path. All exercises are read-only designs or disposable models; do not run cleanup against project or user data.

Read [JanitorWorker](../../src/NetDropbox.Infrastructure/Processing/JanitorWorker.cs), [UserStorageAccount](../../src/NetDropbox.Domain/UserStorageAccount.cs), [BlobObject](../../src/NetDropbox.Domain/BlobObject.cs), and [the persistence model](../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs). The worker runs a sweep, catches and logs exceptions from it, then waits fifteen minutes. It creates a fresh scope for each sweep and obtains the database context and chunk storage there.

## A sweep has a captured time

SweepAsync captures system UTC time once. It finds sessions whose status is Open and expiry is at or before that value. It then marks each Expired and calls external chunk deletion. After processing sessions, it computes a thirty-day trash cutoff, selects old deleted nodes, removes them, and saves changes once at the end.

Capturing one now value makes the selection internally easier to explain: all expiry and retention predicates in that sweep refer to the same observation. It does not freeze the database or stop other operations. A completion may race with the sweep after the query has materialized sessions. The actual persistence concurrency policy determines which conflicting updates are detected.

Boundary equality matters. A session expiring exactly at now is eligible. A trash node whose DeletedAt equals the cutoff is also eligible. These are precise predicates for deterministic tests. A test using only obviously old timestamps can miss an accidental strict-versus-inclusive comparison change.

## External deletion occurs before database saving

The worker mutates session status in memory and deletes chunk data before SaveChanges. If deletion succeeds and the eventual save fails, the database can still describe an Open session whose files have been removed. If deletion fails midway through the session loop, earlier external deletions may already have occurred while none of the tracked status changes are saved.

This is another example of multiple resource boundaries. Wrapping the database save in a transaction does not restore deleted files automatically. A recovery design may choose to persist intent before deletion, perform idempotent cleanup, and mark completion afterward. The current code's ordering should be characterized before proposing that change.

DeleteSessionAsync itself checks whether the directory exists, which makes repeated deletion of an already-absent directory locally tolerable. That property helps retries but does not prove the directory is safe to delete while an upload writer is active. Ownership and expiry coordination remain relevant. A stale session object can represent a state that changed after the query.

## Trash removal is not quota release in this method

The janitor removes FileNode records selected as old trash. The model declares cascading deletion from FileNode to its Versions. The inspected sweep does not call UserStorageAccount.Release or BlobObject.RemoveReference. It also does not call blob-storage deletion. Therefore this path alone does not establish that quota accounting or physical content reclamation follows permanent trash removal.

Do not fill the gap by assuming the database cascade runs domain methods. A relational cascade deletes related rows; it does not ordinarily invoke C# object methods such as Release for each deleted version. The actual mapping and database behavior must be distinguished from domain lifecycle logic. A test that merely counts removed file rows cannot prove correct accounting.

The meaning of quota must also be defined. If UsedBytes counts every retained version, releasing only the current version size would be wrong. If it counts unique physical content per user, logical copies and shared blobs complicate the rule. The current completion path reserves each completed session's total size, while copy does not visibly reserve quota in its method. A complete accounting policy needs to reconcile those operations explicitly.

## Build an obligation ledger

For each permanent deletion candidate, record owner, version identities, blob references, logical bytes charged, physical bytes eligible for reclamation, shares, and processing jobs. Mark which resources are handled by the actual sweep and which require another actor or an unimplemented policy. This is more useful than one checkbox called deleted.

Suppose two file versions point at one blob. Removing one file should not necessarily remove the shared bytes. If both are deleted, reclamation still needs to account for other references and active writes. A reference count can support the decision only if its maintenance is reliable across all create, copy, restore, and delete paths. Counting references from authoritative rows may be an alternative reconciliation technique with its own cost.

The exercise should avoid destructive experimentation. A pure ledger model can represent rows and obligations, and a database fixture can use an isolated temporary database if later authorized. There is no need to delete real uploaded files to learn why the order matters.

## Cancellation has two locations

The outer loop checks the stopping token and passes it into SweepAsync. A cancellation during a sweep can be caught by the broad exception handler and logged as a sweep failure. The following Task.Delay also uses the token and sits outside that catch. Cancellation there can end the background task through the normal cancellation path. These are different code locations with different logging behavior.

The chunk adapter's DeleteSessionAsync accepts a token but performs synchronous filesystem operations without checking it. Passing a token through an interface is not proof that every operation cooperates with cancellation. A cancellation proposal should identify actual observation points and avoid promising reversal of deletions already completed.

A controlled test can cancel before selection, during an adapter gate, or before final SaveChanges. Record which external deletions occurred and which database changes committed. A single assertion that the task was cancelled does not explain the remaining resource state.

## Exercise ND-12A: draw a partial sweep

Use three expired sessions and make deletion fail on the third. List in-memory statuses, saved statuses, and chunk-directory outcomes under the current order. Then move the failure to final SaveChanges. Explain why an ordinary database rollback cannot reconstruct earlier deleted chunk bytes.

## Exercise ND-12B: define permanent-deletion accounting

Choose a quota meaning and work through a file with two versions sharing one blob plus another file referencing the same blob. State what should happen to logical usage, reference count, and physical storage when one file is purged. Identify which current sweep statements implement those obligations and which do not.

## Exercise ND-12C: propose safe cleanup intent

Design an idempotent cleanup-intent record with ownership, eligibility, progress, and retry state. Include a completion racing with expiry and an active writer. Explain the authoritative check before deletion and the evidence needed for a real integration test. Do not execute deletion commands.

## Review checkpoint

A strong answer treats cleanup as a recoverable workflow with accounting consequences. It recognizes that a cascade does not execute domain bookkeeping, a token does not guarantee cooperation, and an external deletion cannot be rolled back by database syntax alone. Its proposed design makes every outstanding obligation visible.
