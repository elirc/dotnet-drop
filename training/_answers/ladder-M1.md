# ANSWER — M1 idempotent upload completion

**Q1 interleaving.** T1 and T2 both load the session (`CompleteUpload.cs:30`), and both see `Status == Open` (`:33`). Both hash about 40 s. T1 finds no blob → saves the blob and adds the row. T2 also finds no blob (T1 hasn't committed) → saves again (idempotent at the storage level) and adds a row too. At commit, the unique `ContentHash` index (`NetDropboxDbContext.cs:62`) fails T2 → 500. If the blob already existed, both call `AddReference`, both `AddVersion` (v2, v3), both `Reserve`, both commit, and nothing conflicts, because the `Version` token never changes (review #10) and `UploadSession` has no token. **The guard is line 33.**

**Q2 options.**
| Option | Holds lock during hash? | Crash after claim |
|---|---|---|
| `FOR UPDATE` on session row for the whole handler | yes (40 s, one connection pinned) | lock released by rollback; retry works |
| Atomic claim `UPDATE … SET status='Completing', claimed_at=now() WHERE id=@id AND status='Open'` (rows==1 wins) | no | session stuck in Completing → need **lease**: allow re-claim if `claimed_at < now()-5min` |
| `xmin` row version (`IsRowVersion()`) | no | loser gets `DbUpdateConcurrencyException` at the *end*, after hashing; wasteful but correct |
| Idempotency-Key table | no | same lease problem; good for generic endpoints, overkill here since session ID *is* the key |

**Reference choice:** an atomic claim plus a lease, then do the work, then a single `SaveChanges` that sets `Completed` and `CompletedFileNodeId`. The loser sees `Completing` → returns 409 `{code:"completing"}`. The client polls `GET /uploads/{id}` (J1), or sees `Completed` → loads `CompletedFileNodeId` and returns the **same DTO** with 200.

**Q3 boundary.** Today's order: blob write (outside the DB) → a DB transaction (blob row, node, version, job, session, user) → chunk delete → enqueue (outside the DB). Correct order: blob write first (content-addressed, so it's idempotent and safe to repeat), then the DB transaction, which includes an **outbox or job row** that the worker reads (S1), not a best-effort enqueue. Chunk delete last and idempotent (the janitor can also clean it up).

**Q4 matrix (after the fix).**
| Crash point | State left | Retry does |
|---|---|---|
| after blob saved | blob object without row (orphan) | re-claim after lease → finds object, `SaveAsync` no-op, continues. The orphan is harmless: GC via M4 reconciliation |
| after `SaveChanges` | session Completed, job row Queued | retry sees Completed → returns the existing node; the worker picks up the job from the DB (S1) |
| after chunk delete | same as above | same |
| after enqueue | done | same |

Today's code differs in row 2: the message is lost from the Channel, and the file stays PendingScan forever (review #6).

**Q5.** `UploadSession.CompletedFileNodeId` already exists (`UploadSession.cs:32`). The handler should short-circuit at the top: `if (session.Status == Completed) return Map(await db.FileNodes.FindAsync(session.CompletedFileNodeId))`.

**Test.** Testcontainers PG: two `Task.Run`s with separate scopes and a `TaskCompletionSource` barrier after the claim point (inject a hook or a slow fake `IChunkStorage`). Assert 1 version, `used_bytes` += size once, `ref_count` += 1, 1 job, and both responses carry the same `Id`.
