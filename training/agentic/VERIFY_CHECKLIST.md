# Verifying AI output in NetDropbox

Tick each item only with evidence you produced yourself.

## Run it
- [ ] `dotnet build src/NetDropbox.Api/NetDropbox.Api.csproj` passes on a **clean worktree** (not the tree the agent edited, where ignored files may hide gaps, as review #1 proves).
- [ ] `dotnet test` output pasted, with the test counts. The count went **up** by the number of tests the spec lists.
- [ ] The concurrency test ran against Postgres. Search the output for Testcontainers container start lines. If Docker isn't available, the PR says "concurrency test not executed".

## Read the diff
- [ ] Every changed file is in the allowed list. No stray edits to `appsettings.json`, `.gitignore` or `docker-compose.yml`.
- [ ] No new `?? "default"` fallbacks for config or secrets (INC-005 pattern).
- [ ] No `catch (Exception) { }`, and no swallowed `DbUpdateConcurrencyException` without a decision.
- [ ] No `Task.Delay` or sleep inside request or consumer paths (INC-002 pattern).
- [ ] Every query that loads a user-owned row still filters by owner (INC-003 pattern). Grep `OwnerId ==` before and after.

## Check claims against code
- [ ] For each sentence in the agent's summary ("idempotent", "atomic", "thread-safe"), find the line that makes it true. If you can't point at one, the claim is false until proven.
- [ ] If the agent says a concurrency token protects something, check that the token's value actually changes: `IsRowVersion()`/`xmin` or explicit assignment. `IsConcurrencyToken()` on a never-written column protects nothing (review #10).
- [ ] If a test uses EF InMemory, confirm it isn't the test claiming to prove uniqueness, locking, transactions or SQL translation. InMemory can't prove any of those.

## Fake or placeholder behavior
- [ ] No `// TODO`, `NotImplementedException`, hard-coded IDs, or tests that assert only `Should().NotBeNull()`.
- [ ] Tests fail when the fix is reverted. Actually revert the production change, run, and see red.

## Data and migration
- [ ] Any schema change: is it reversible? What happens to rows written by old code during a rolling deploy?
- [ ] With no migrations in this repo (`EnsureCreated`), did the agent change the model in a way an existing DB won't pick up? Write down how it will reach prod.
