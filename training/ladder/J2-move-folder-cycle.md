# J2 — Stop folders being moved into themselves

**Context.** `FileNode.Move` (`src/NetDropbox.Domain/FileNode.cs:44-49`) only stops a *file* from containing itself. `FileService.MoveAsync` (`src/NetDropbox.Application/Files/FileService.cs:52-62`) checks that the destination is the caller's own folder, but not that it isn't inside the folder being moved.

**Ticket (support escalation).** "A customer moved 'Projects' into 'Projects/2026/Q3' by drag-and-drop mistake. The whole Projects tree vanished from their root. They think we deleted their data."

**Constraints.** Folder depth in prod is at most about 30. Don't load the user's whole tree into memory. Keep the rule in the domain if you can, and say where the data it needs comes from.

**Definition of done.**
- Moving a folder into itself or any descendant is rejected. Choose 400 or 409 and justify it.
- A domain unit test for the self case, and an application test for a 3-level descendant case.
- One SQL query that finds trees **already** detached by this bug in prod, plus a note on how support re-attaches them.

**Explain before touching.**
1. After the bad move in the ticket, which rows changed, and why does the UI show nothing? (`ListAsync` filters on `ParentId`.)
2. Compare three ways to find ancestors: a parent-lookup loop, a recursive CTE, or a materialized path column. What does each cost per move, and which fits this repo today?
3. Two concurrent moves (A into B while B into A) can both pass your check. Does that matter here, and what would stop it?
4. What does the Domain layer know, and what must Application pass into it?

<details><summary>Hints</summary>

- `WITH RECURSIVE anc AS (SELECT id, parent_id FROM netdropbox.file_nodes WHERE id = @dest UNION ALL SELECT n.id, n.parent_id FROM netdropbox.file_nodes n JOIN anc ON n.id = anc.parent_id) SELECT EXISTS (SELECT 1 FROM anc WHERE id = @moving);`
- Detached-tree detection is the same CTE walking *down* from roots. Anything not reached is detached.
</details>

Answer: `training/_answers/ladder-J2.md`
