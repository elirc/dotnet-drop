# NetDropbox agentic practice: 4 drills

Each drill turns an existing `training/` item into practice at directing an agent. The loop is always the same:
1. **You** diagnose (no agent) and write the spec or prompt. The spec is the deliverable you're graded on.
2. **An agent** implements it in a worktree.
3. **You** review adversarially against the checklist at the bottom, then against the sealed rubric in `learn/_answers/01-agentic-practice.md`.

Background: *05 verify before trust* (external apprenticeship note, not in this repo). The long-form version of this loop, on ladder M1, is `training/agentic/EXERCISE.md`. Do these drills first, then that one.

Setup for every drill: `git worktree add ../netdrop-drill-X <branch>` from your clone. Never let an agent work in your main tree.

> **Status (2026-10-06):** the incident branches that drills A and B start from (`training/incidents/003-*`, `004-*`) were never pushed. A fresh clone has only `origin/main`. You can still write each drill's spec (the "You write" part is the skill being trained), but the "must FAIL on the incident branch" verification can't be done. Drill C targets `main` and is fully doable. Its fix doesn't need the incident branch. Note that `main` itself won't build until the storage adapters are written (`learn/README.md`, "Known state").

---

## Drill A — a query-shape regression test for INC-004 (60 min)
**Source:** `training/incidents/004-slow-folder-listing.md`, branch `training/incidents/004-slow-folder-listing`.
**Precondition:** you've done the incident by hand (Session 7) and know the two regressions.

**You write** a 10-line spec for *one test* that would have blocked the PR. It must state:
- what it counts or inspects (commands? SQL text? columns?)
- which database provider it runs on, and **why that provider can prove the thing**
- the seed size and the exact assertion threshold
- where the file goes (`tests/NetDropbox.Tests/...`) and which project references it needs that the test project lacks (at `d612e0b` it references only Domain and Application; on current `main` it already references Infrastructure).

**Agent does:** writes the test and any helper (interceptor, fixture). Paste your spec verbatim, and add: "Run the test on this branch and paste the real output. It must FAIL here."

**You verify:** the test fails on the incident branch and passes after you revert the PR's `FileService.ListAsync` change. Then check the trap in the rubric: does the provider the agent picked actually execute SQL commands?

## Drill B — an authorization-matrix test for INC-003 (75 min)
**Source:** `training/incidents/003-shared-download-exposure.md`, branch `training/incidents/003-shared-download-exposure`.

**You write** the matrix before the agent sees anything: rows are every **read** method in `src/NetDropbox.Application/Files/FileService.cs` and `src/NetDropbox.Application/Shares/ShareService.cs`, columns are {owner, direct-share recipient, stranger}, and each cell holds the expected result (data / 404). Decide the recipient's expected result for each method yourself. Some are product decisions (can a Viewer download? `DirectShareRole` is stored but never read). Write your decision into the spec.

**Agent does:** turns your matrix into a table-driven xUnit test (`[Theory]` + `MemberData`) against a real query provider, seeding exactly: owner A, file F, share A→B, stranger C.

**You verify:** every cell in your matrix appears in the test, the test fails on the incident branch for exactly the cells the bug breaks, and the agent didn't "fix" a failing cell by changing your expected value.

## Drill C — fail-closed JWT configuration for INC-005 (60 min)
**Source:** `training/incidents/005-staging-token-anomaly.md`, branch `training/incidents/005-staging-token-anomaly`.

**You write** a prompt that fixes **the day-one defect on `main`**, not just the PR: the silent fallback key in `Program.cs:42` and `AuthController.cs:32`. The prompt must name:
- the options class and the config section it binds to
- the validation rules (length, "not the dev key outside Development")
- that startup must fail (`ValidateOnStart`)
- every file allowed to change, including the deployment files outside the C# code (`docker-compose.yml`, `.env.example`)
- the test that proves a Production host without a key refuses to start.

**Agent does:** implements it on a branch from `main`, not from the incident branch.

**You verify:** with the checklist below, especially the grep items. Then answer in writing: if this fix had been on `main` before PR #227, what would Tuesday's deploy have looked like?

## Drill D — ladder M3, "scan state follows the bytes" (90 min; a security invariant)
**Source:** `training/ladder/M3-scan-follows-bytes.md` (read its "Explain before touching" and answer it first).

**You write:** one sentence stating the invariant in terms of *versions and blobs*, not nodes. Then list **every** code path that serves or copies bytes. The ticket names four, but find them yourself with `grep -rn "OpenReadAsync\|MarkScanClean" src`. Then write the spec: one authoritative check, the copy decision (refuse vs. inherit status), and how restore-version gets a scan queued.

**Agent does:** implements your spec, plus tests for each path.

**You verify:** for each path in your list, a test that serves an Infected or Pending version gets refused. Revert the central check and confirm *all* of those tests go red, not just one. The rubric lists the paths agents most often miss.

---

## Verification checklist: before you trust agent output in NetDropbox
Use `training/agentic/VERIFY_CHECKLIST.md` in full. These are the NetDropbox-specific checks it doesn't spell out, learned from B01's findings:

- [ ] **Clean-clone build.** Build a *fresh clone* of the agent's branch, not the agent's worktree. Ignored files on disk hid a non-building `main` for the whole life of this repo (`.gitignore:8`).
- [ ] **The provider can prove the claim.** EF InMemory runs no SQL. It can't prove query shape (Drill A), uniqueness (J3), locking (M1) or transactions. A `DbCommandInterceptor` never fires on it.
- [ ] **Owner predicates.** Count `OwnerId ==` in `src/NetDropbox.Application` before and after (`grep -c`). Any read path that lost one is INC-003 again. `GetOwnedNode` (`FileService.cs:143-147`) is the house pattern.
- [ ] **No secret fallbacks.** `grep -rn '?? "' src` shows no new defaults for keys, passwords or connection strings (`Program.cs:42`, `:74` and `AuthController.cs:32` are the existing ones).
- [ ] **The concurrency token is real.** If the agent says "optimistic concurrency", it must be `xmin`/`IsRowVersion()` or explicit increments. `IsConcurrencyToken()` on `Entity.Version` is inert (`Entity.cs:8`).
- [ ] **The side effects after `SaveChangesAsync` are accounted for.** Any change to `CompleteUpload.cs` says what happens on a crash between `:82` and `:84`.
- [ ] **Both token sites.** Anything touching JWT changes issuing (`AuthController.cs:30-45`) and validation (`Program.cs:41-57`) together.
- [ ] **Your working tree is untouched.** `git status --short` in your main clone shows the same entries as before the drill (empty, on a fresh clone).
- [ ] **Red on revert.** Every new test fails when you revert the production change. Paste both outputs.
