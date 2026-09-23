# J3 — Name conflicts on rename, move and copy

**Context.** `CreateFolderAsync` checks for a same-named sibling (`src/NetDropbox.Application/Files/FileService.cs:36-37`). `RenameAsync`, `MoveAsync` and `CopyAsync` don't. `CompleteUploadHandler` treats a same-named file as "add a new version" (`src/NetDropbox.Application/Uploads/CompleteUpload.cs:63-72`). The index on `(OwnerId, ParentId, Name, IsDeleted)` (`NetDropboxDbContext.cs:38`) is **not unique**.

**Ticket.** "Users end up with two `report.pdf` files in one folder. Downloads and new uploads then pick one arbitrarily. Names must be unique among live siblings, case-insensitively, because Windows and macOS users expect that."

**Constraints.** Trashed items must not block names. Existing duplicates in prod must not make the rollout fail. Root items have `ParentId = NULL`.

**Definition of done.**
- Rename, move and copy into a conflicting name return `409` with a machine-readable `code`.
- A DB-level guarantee, not only a pre-check, mapped to 409 when it's violated.
- A plan for the duplicates already in prod.
- Tests: one per operation, plus one that proves the DB constraint catches the race the pre-check misses.

**Explain before touching.**
1. Why is the `AnyAsync` pre-check alone insufficient? Write the interleaving.
2. Write the Postgres index. How do you handle a `NULL` parent and case-insensitivity?
3. This repo has no migrations (`DbSeeder.cs:14` uses `EnsureCreated`). How does the index reach an existing database?
4. How do you surface Npgsql `23505` as 409 without leaking SQL? Which layer maps it?
5. What happens to the upload rule "same name means new version" once names are case-insensitive?

<details><summary>Hints</summary>

- A partial expression index: `(owner_id, coalesce(parent_id, '0000…'), lower(name)) WHERE NOT is_deleted`. Or `NULLS NOT DISTINCT` (PG15+) plus a `citext` or collation choice.
- `catch (DbUpdateException { InnerException: PostgresException { SqlState: "23505" } })`
</details>

Answer: `training/_answers/ladder-J3.md`
