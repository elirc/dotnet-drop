SEALED — attempt first. Open this only after you've written your spec and reviewed the agent's output.

# Rubrics: NetDropbox agentic drills

Score each drill out of 10. **The spec is worth 6 points and the review 4.** Agent code that happens to be good earns you nothing. Your spec and your catches do.

## Drill A — INC-004 query-shape test
**Spec points (6)**
- 2 pts: it counts **DB commands** (a `DbCommandInterceptor` incrementing in `ReaderExecuting`/`ReaderExecutingAsync`) and/or asserts the SQL text doesn't contain `extracted_text`. That covers both regressions (N+1 and over-fetch), per `training/_answers/incident-004.md`.
- 2 pts: it names a **relational provider that executes SQL**. Testcontainers Postgres is the reference: `Testcontainers.PostgreSql` is already referenced in `NetDropbox.Tests.csproj` and unused. SQLite in-memory is acceptable only with a stated caveat: SQLite ignores `HasDefaultSchema("netdropbox")` and translates some queries differently. Whether this model runs cleanly on SQLite is UNVERIFIED.
- 1 pt: a concrete seed and threshold, e.g. 50 files in one folder and `commands <= 2`. It's a shape assertion, **not a timing assertion**, so it isn't flaky.
- 1 pt: it names the missing references: `NetDropbox.Infrastructure` (for `NetDropboxDbContext`) and `Npgsql.EntityFrameworkCore.PostgreSQL`. On `main` that also means J0 first, because Infrastructure doesn't build without the Storage adapters. Incident branches carry them.

**Review points (4)**
- 2 pts, **the trap**: if the agent used `UseInMemoryDatabase`, the interceptor **never fires** (InMemory runs no commands). The count stays 0, `0 <= 2` passes, and the test is green on the bugged branch. It "passes" and proves nothing. You catch this only by running it on the incident branch and seeing green where you expected red.
- 1 pt: you checked that it's red on the branch and green after reverting the PR change, with both outputs pasted.
- 1 pt: you rejected any agent "improvement" to `ListAsync` itself. The spec asked for a test, not a fix. Scope creep in agent output is a review finding.

## Drill B — INC-003 authorization matrix
**Spec points (6)**
- 2 pts: the rows cover every read path. `FileService`: `ListAsync`, `VersionsAsync`, `OpenDownloadAsync` (with and **without** `versionId`), `SearchAsync`, `UsageAsync` (the user's own only), `CopyAsync` (it reads the source), `RestoreVersionAsync`. `ShareService`: `SharedWithMeAsync`. A spec that lists only download scores 0 here, because the PR changed two methods and reviewers pattern-matched between them.
- 2 pts: explicit product decisions for the recipient column. Reference: a recipient can list and download the shared node; *Viewer vs Editor* should be decided and written down, because `DirectShareRole` is read nowhere in `src/` (INC-003 answer). A stranger gets **404, not 403**, on every row (don't leak existence, as `GetOwnedNode` does at `FileService.cs:143-147`).
- 1 pt: the fixture is exactly A, F, share A→B, C, and it also includes a second file F2 with **no** share. INC-003's 200/404 split depends on whether *any* share row exists.
- 1 pt: a real query provider. `db.DirectShares.Any(...)` inside the predicate is an EF expression that no domain test executes.

**Review points (4)**
- 2 pts: on the incident branch, with the reference decisions (the recipient may download), the failing cells are exactly "stranger × `OpenDownloadAsync` on F" (the file with a share row). If you decided a recipient may *not* download, the recipient cell fails too, and that's consistent. If more cells fail, the fixture is wrong. If none fail, the provider or the seed is wrong.
- 1 pt: you diffed expected values between your spec and the test. Agents commonly "resolve" a red cell by editing the expectation. Any changed expectation without your sign-off is a rejected change.
- 1 pt: you noticed that `SearchAsync` and `ListAsync` filter strictly by `OwnerId` (`FileService.cs:15,129`), so a recipient gets *nothing* there. You decided whether that's intended (shared files appear only under `SharedWithMeAsync`) and wrote it down.

## Drill C — fail-closed JWT config
**Spec points (6)**
- 1 pt: one `JwtOptions` bound once and used by **both** `AuthController` (issuing, `:30-45`) and `Program.cs` (validation, `:41-57`). Today they read config separately and can drift.
- 2 pts: `.Validate(key length >= 32)`, `.Validate(not the dev key unless IsDevelopment())`, `.ValidateOnStart()`. The literal `"local-development-signing-key-change-me-32chars"` must disappear from `src/`.
- 1 pt: the dev key moves to development-only config. There's no `appsettings.Development.json` on `main`, so the spec must say to create it or use user-secrets, and to remove the key from base `appsettings.json`.
- 1 pt: the deployment files are in scope: `docker-compose.yml` (`Jwt__*` env vars) and `.env.example`. A config contract lives outside the C# diff; that's the INC-005 lesson.
- 1 pt: the test builds the host with `ASPNETCORE_ENVIRONMENT=Production` and no key, and asserts that startup throws `OptionsValidationException`. A second case sets the dev key in Production and asserts the same.

**Review points (4)**
- 1 pt: `grep -rn '?? "' src` shows no remaining fallback for the key. Issuer and audience defaults are a judgment call; say which you kept and why.
- 1 pt: you checked that `Program.cs:74` and `DependencyInjection.cs:20` (the connection-string fallbacks with a password) are either handled or explicitly deferred, not silently ignored.
- 1 pt: the Tuesday answer. The new pods crash at startup, the rollout health check fails, the old pods keep serving, and it's a SEV-4 with zero logouts, instead of a self-resolving alert followed by a public-key breach.
- 1 pt: `git diff --stat` touches only the files your spec allowed.

## Drill D — M3 scan state follows the bytes
**Spec points (6)**
- 2 pts: the invariant is stated on the **version or blob**: "bytes are served or copied only if the scan result for *that version's content* is Clean". Not "the node is Clean", because node-level status is exactly the bug.
- 2 pts: the path list from the grep. Byte servers: `FileService.OpenDownloadAsync` (`:114-123`; the `versionId` branch bypasses the node check) and `ShareService.OpenPublicDownloadAsync` (`:53-68`). Status writers: `FileService.CopyAsync` (`:76`, launders Infected or Pending), `FileService.RestoreVersionAsync` → `FileNode.RestoreVersion` (`:68`, sets Pending and enqueues nothing, so it's stuck forever), and `ProcessingWorker` (`:47`, `:62`).
- 1 pt: the copy decision is justified. Reference: the copy inherits the source version's real status, and a Pending copy is fine; or refuse Infected and Pending with 409. Either works if written down with the user-visible consequence.
- 1 pt: restore-version enqueues a scan only if that blob has no Clean result yet (dedupe: scan per content, per the ticket's constraints).

**Review points (4)**
- 2 pts, **the path agents most often miss**: `ProcessingWorker.cs:62` marks the **node** Clean based on the scan of `message.FileVersionId`, and it never checks that this version is still `node.CurrentVersionId`. If v2 is uploaded while v1's scan is running, v1's Clean result flips the node to Clean while v2 is unscanned. This is a code-reading deduction, not reproduced (UNVERIFIED at runtime). A fix that moves status to the version closes it. One that keeps node status needs an explicit current-version guard.
- 1 pt: reverting the central check turns **every** path test red. If one stays green, that path bypasses the check.
- 1 pt: you confirmed that the public-share path now uses the same check, not a second hand-written copy (`ShareService.cs:63` today duplicates the logic).

## Checklist items graders care most about
If you ticked "clean-clone build", "provider can prove the claim" and "red on revert" with pasted evidence, you've learned the core of this repo's agentic lesson. Those three are what B01 found missing from the repo's own 47-file `docs/upskill/` curriculum, which describes an app that never compiled ([curriculum 05](../../../opusorganize/apprenticeship/curriculum/05-verify-before-trust.md)).
