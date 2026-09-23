# ANSWER — J2 folder move cycle

**Q1.** One row changes: `file_nodes` for "Projects", with `parent_id` set to the Q3 folder's ID (a descendant of Projects). Now Projects → … → Q3 → Projects is a cycle, and no node in it has `parent_id IS NULL` or a parent outside the cycle. `ListAsync` at root (`parent_id IS NULL`) no longer returns Projects, and nothing in the UI can navigate into the cycle. Search (`SearchAsync`) still finds the files, because it doesn't filter by parent. That's the fastest way to reassure the customer that the data exists.

**Q2.**
| Approach | Cost per move | Fit |
|---|---|---|
| Loop of parent lookups from dest upward | ≤ depth queries (≤ 30) | Simple and fine at this scale. N round-trips. |
| Recursive CTE | 1 query | The best fit here: no schema change. |
| Materialized path (`/a/b/c`) or ltree | 1 indexed query, but a move must rewrite the path of the whole subtree | Worth it only if subtree queries become common (M2 search in a folder, S2). |

**Q3.** A then B concurrently: T1 moves A under B and checks that B's ancestors don't contain A; T2 moves B under A and checks that A's ancestors don't contain B. Both pass, and both commit a cycle. It's rare (same user, two tabs), but real. Mitigations: `SELECT … FOR UPDATE` on the user's row (serializes all moves for one user, which is cheap here); or SERIALIZABLE isolation plus retry; or re-check after the update inside the same transaction.

**Q4.** The domain knows "a node can't be its own parent" and can accept an `IReadOnlyCollection<Guid> destinationAncestors`: `Move(Guid? parentId, IReadOnlyCollection<Guid> ancestorsOfDestination)` throws if `Id == parentId || ancestors.Contains(Id)`. Application computes the ancestors with the CTE. Also fix the existing bug: the self-check applies to folders, not only files (`FileNode.cs:46`).

**Detached-tree query (prod):**
```sql
WITH RECURSIVE reachable AS (
  SELECT id FROM netdropbox.file_nodes WHERE parent_id IS NULL
  UNION ALL
  SELECT n.id FROM netdropbox.file_nodes n JOIN reachable r ON n.parent_id = r.id)
SELECT n.id, n.owner_id, n.name, n.parent_id FROM netdropbox.file_nodes n
WHERE n.id NOT IN (SELECT id FROM reachable) AND n.type = 'Folder';
```
This also catches children orphaned by janitor purges (no FK on `parent_id`; review #13). Support re-attaches a detached folder by setting its `parent_id` to NULL (root) with the owner's consent.

**Status code:** 409 means the request conflicts with the current state of the tree. 400 is also defensible, since the input is invalid regardless of state. Pick one and document it; consistency matters more than the choice.
