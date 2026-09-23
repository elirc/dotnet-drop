# ANSWER — J3 name conflicts

**Q1.** T1 and T2 both rename a file to `report.pdf` in folder F. Both run `AnyAsync` → false. Both `SaveChanges`. Now there are two live siblings with that name. Also, `CreateFolderAsync`'s own check compares case-sensitively (`x.Name == name`), so `Report.pdf` vs `report.pdf` already slips through.

**Q2 index.**
```sql
CREATE UNIQUE INDEX CONCURRENTLY ux_file_nodes_live_sibling_name
ON netdropbox.file_nodes (owner_id, COALESCE(parent_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(name))
WHERE NOT is_deleted;
```
Without the `COALESCE`, two root items with the same name wouldn't conflict (NULLs are distinct). `NULLS NOT DISTINCT` on PG15+ is the cleaner alternative, but it can't apply to an expression over `lower()` together with the NULL column in quite the same way, so the COALESCE form is simpler. `lower()` versus `citext`: `lower` is fine for ASCII. Unicode case folding differs across ICU/libc; note it as a known limit.

**Q3.** There are no migrations. Either (a) do S2's baseline first (the right answer long-term), or (b) ship a raw SQL script run by the deploy step, idempotent via `IF NOT EXISTS`. Use `CONCURRENTLY` so writes aren't blocked. It can't run inside a transaction, so EF migrations need `migrationBuilder.Sql(..., suppressTransaction: true)`.

**Existing duplicates:** the index build will fail on them. First run `SELECT owner_id, parent_id, lower(name), count(*) … HAVING count(*) > 1`, then rename the newer duplicates to `name (2).ext` (the product's call; tell the users), then build the index.

**Q4.** Mapping in Application, where the use-case knows what a conflict means:
```csharp
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "ux_file_nodes_live_sibling_name" })
{ throw new AppException("A file or folder with that name already exists.", HttpStatusCode.Conflict); }
```
Application would then reference Npgsql, which leaks infrastructure. The cleaner version is an `IUniqueViolationDetector` port, or an Infrastructure `SaveChangesInterceptor` that rethrows a neutral `UniqueConstraintException(constraintName)`. Keep the pre-check anyway for a friendly message on the common path.

**Q5.** `CompleteUpload.cs:65` looks up with `x.Name == session.FileName` (case-sensitive). Uploading `Report.PDF` next to `report.pdf` would try to *create* a new node and hit the unique index → 409 at the end of a large upload. Change the lookup to `lower(name)` so it becomes a new version, and check early in `StartUploadHandler` to fail fast before bytes are sent.

**Tests.** One per operation. For the race: two DbContexts on Testcontainers Postgres, both pass the pre-check, and the second `SaveChanges` throws and is mapped to 409. InMemory can't prove this, because it has no unique expression index.
