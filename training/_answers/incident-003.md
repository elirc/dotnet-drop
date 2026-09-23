# ANSWER — INC-003 shared-download exposure (IDOR)

## Root cause
In `FileService.OpenDownloadAsync` the new predicate is
`x.OwnerId == userId || db.DirectShares.Any(s => s.FileNodeId == x.Id)`.
It drops `&& s.SharedWithUserId == userId`. The `VersionsAsync` change in the same PR has the filter, which is why the researcher's `/versions` call returned 404. Reviewers read the first hunk, saw it was right, and pattern-matched the second.

**Effect:** any authenticated user can download any non-deleted, scan-Clean file that has been direct-shared with *anyone*. File `3d9a0c2e` has a `direct_shares` row (shared with the CFO), so it's exposed. `81be44f1` has none, so it returns 404. The SQL in the brief explains the 200/404 split on its own.

Exploitability: the attacker needs a node GUID. GUIDs aren't secret. They appear in URLs, in `FileNodeDto.Id` in every listing, in share-link responses and in screenshots. "Unguessable IDs" is not an access-control mechanism.

## Blast radius
- Exposed set: `SELECT DISTINCT n.* FROM file_nodes n JOIN direct_shares s ON s.file_node_id = n.id WHERE NOT n.is_deleted AND n.scan_status='Clean';` for the window from the PR #224 deploy to the fix.
- Actual abuse: you need *who downloaded what*. Serilog request logging (`Program.cs:88`, `UseSerilogRequestLogging`) records path and status but **not the user ID**. The correlation middleware doesn't add it either. Join to the owner via the JWT subject? Not logged. So you probably **can't prove non-exploitation**. Say that in the disclosure and fix the logging: enrich `UserId` into `LogContext` after authentication, plus an audit table for downloads.

## Fix
```csharp
x.OwnerId == userId || db.DirectShares.Any(s => s.FileNodeId == x.Id && s.SharedWithUserId == userId)
```
Better: one reusable predicate, e.g. `static Expression<Func<FileNode,bool>> ReadableBy(Guid userId, IQueryable<DirectShare> shares)`, used by list, versions, download and search. Also decide whether the **Editor/Viewer role** matters for download. `DirectShareRole` is stored but read nowhere in `src/`.

## Regression test
It must run against a **real query provider**. The predicate is an EF expression, so a domain unit test never executes it. Handler-level test with a DB (EF InMemory will evaluate this `Any`; Testcontainers Postgres is better): seed owner A, file F, a share A→B. Assert B gets 200, a third user C gets 404 (`AppException` with NotFound), A gets 200. Add the same matrix for `VersionsAsync`. Better still, a table-driven **authorization matrix test** over every `FileService` method × {owner, share recipient, stranger}.

## Structural options (pick one, state the trade-off)
- **EF global query filter** keyed on the current user: hard to forget, but it hides data from background workers and admin paths unless you use `IgnoreQueryFilters()`, and it's easy to misuse across scopes.
- **Postgres RLS**: enforced even for raw SQL. Needs per-connection `SET app.user_id`, which is awkward with pooling, and harder to test.
- **Central `IFileAccessPolicy.CanRead(userId, nodeId)`** plus an authorization-matrix test: explicit, but you still have to remember to call it.

## Postmortem
- **Severity SEV-1**, window about 17 h. The review missed it because of the inconsistent twin hunks, no tests in the PR, and a PR description that talked only about the happy path.
- **Actions:** fix plus an authz matrix test; user-ID enrichment in logs and a download audit trail; add a CODEOWNERS rule for any predicate touching `OwnerId`; a disclosure to the owners of exposed files.
