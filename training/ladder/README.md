# NetDropbox ladder

Eleven tickets, all against committed `main`. Work in a worktree on your own branch: `git worktree add ../netdrop-ladder -b training/ladder/<ticket> main`.

Every ticket starts with the **Explain-before-touching** gate. Write your answers in `training/ladder/notes/<ticket>.md` *before* you or an agent edit code. The sealed answer in `training/_answers/ladder-<id>.md` compares your prediction with a reference analysis.

> Prerequisite for every code ticket: `main` doesn't build or boot as committed (review findings #1–#2). Do **J0** first. It takes about 20 minutes.

| Rung | Ticket | Core skill | Est. |
|---|---|---|---|
| Setup | [J0 Make main build and boot](J0-make-main-build.md) | build hygiene, EF model config | 0.5 h |
| Junior | [J1 Upload status: 404 not 500, and out of the controller](J1-upload-status-404.md) | API design, status codes, layering, tests | 2 h |
| Junior | [J2 Stop folders being moved into themselves](J2-move-folder-cycle.md) | domain validation, recursive query | 3 h |
| Junior | [J3 Name conflicts on rename/move/copy](J3-name-conflicts.md) | DB unique constraint, 409 mapping | 3 h |
| Mid | [M1 Idempotent upload completion](M1-idempotent-complete.md) | concurrency, idempotency, transactions | 5 h |
| Mid | [M2 Search at 2 million files](M2-search-at-scale.md) | query plans, indexes, pagination | 5 h |
| Mid | [M3 Scan state must follow the bytes](M3-scan-follows-bytes.md) | security invariant, data model | 5 h |
| Mid | [M4 Storage accounting that goes down too](M4-storage-accounting.md) | refcounts, GC races, reconciliation | 6 h |
| Senior | [S1 Durable processing pipeline](S1-durable-processing.md) | outbox, queues, retries, DLQ, observability | design 3 h (+ build 8 h) |
| Senior | [S2 Adopt migrations; move scan status to versions with zero downtime](S2-zero-downtime-migration.md) | expand/contract, backfill, rollback | design 3 h |
| Senior | [S3 Run three replicas in the cloud](S3-three-replicas.md) | stateless design, cloud deploy, rollback | design 3 h |

Suggested order: J0 → J1 → J3 → J2 → M1 → M3 → M4 → M2 → S1 → S2 → S3. M1 and S1 pair with incidents 001 and 002. M3 pairs with review findings #3–#4.
