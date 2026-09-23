# ANSWER — INC-004 slow folder listing

## Two regressions
1. **N+1 queries.** One query for the nodes, then `SingleOrDefaultAsync` per node against `file_versions`. A 5,912-item folder costs 5,913 round trips. At ~0.3 ms DB time plus ~0.6 ms network and driver overhead each, that's about 5–6 s. Evidence: statements/s went from 1.4 k to 38 k, the 9.1 M calls of `... FROM file_versions WHERE id = $1 LIMIT $2`, and active connections at 64. Each slow listing holds a pooled connection for seconds, so other routes start waiting on the pool.
2. **Over-fetching.** The node query now materializes whole `FileNode` entities, including `ExtractedText`. `ProcessingWorker.ReadTextSampleAsync` stores up to 1 MB per text/JSON/XML file there (`ProcessingWorker.cs:55`, `FileNode.ExtractedText` mapped as `text`). The old projection never selected it. Evidence: the node query now lists `f.extracted_text`, working set went from 390 MB to 1.6 GB, and Gen2 GCs appeared (large strings go on the LOH).

Search latency didn't move, because it still uses a projection. That's a useful control that points at the list code path.

## Was the premise true?
No. `FileNode.RestoreVersion` (`FileNode.cs:64-70`) only moves `CurrentVersionId` to an existing version of **the same node**. The correlated sub-select `x.Versions.Where(v => v.Id == x.CurrentVersionId)` finds it. "0 B" comes from `formatBytes(0)` in `web/src/api.ts:166-171`: a legitimately empty file, or a folder rendered as a file. Before approving, ask for a failing test or a repro ID. A "fix" without a reproduction is a smell.

## Fix
Revert to a single projected query. If you want to make the intent explicit, use a left join:
```csharp
from n in db.FileNodes.AsNoTracking().Where(...)
join v in db.FileVersions on n.CurrentVersionId equals v.Id into cv
from v in cv.DefaultIfEmpty()
orderby n.Type, n.Name
select new FileNodeDto(n.Id, n.ParentId, n.Name, n.Type, n.ScanStatus, v == null ? 0 : v.Size, v == null ? null : v.MimeType, n.UpdatedAt, n.IsDeleted, n.ThumbnailPath)
```

## Test that catches it again
Count the commands: register a `DbCommandInterceptor` in a handler test that increments a counter in `ReaderExecuting`. Seed 50 files, call `ListAsync`, assert `commands <= 2`. With Testcontainers Postgres you can also assert that the SQL text does not contain `extracted_text`. This is a *query-shape* test, not a timing test, so it isn't flaky.

## Pagination (task 4)
Even the good query returns an unbounded array. At around 10⁴–10⁵ items, the JSON size (~250 B/row) and the React render (`App.tsx:119-128`, no virtualization) dominate. Add keyset pagination on `(type, name, id)` with `?limit=&after=`. For backward compatibility, keep the bare-array response when `limit` is absent (the current client), and return `{ items, nextCursor }` only when it's present, or use a new `/api/v2`. Changing the default response shape would break `api.files()` in `web/src/api.ts:91-96`, which types the response as `FileNode[]`.

## Postmortem notes
Reviewer question: *"What's the largest folder in prod, and how many queries does this make for it?"* Actions: a query-count guard test, a `pg_stat_statements` calls-per-request alert, an index check (`file_nodes (owner_id, parent_id, name, is_deleted)` exists, `NetDropboxDbContext.cs:38`; the `file_versions` PK covers the join).
