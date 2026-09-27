# Learn NetDropbox: study guide (B08)

This folder is the on-ramp to the lab in `training/`. `training/` was written for mid-to-senior practice. This guide starts from junior fundamentals and shows you how to work through `training/` in the agentic era: you write the spec, an agent types, and you decide whether to trust the result.

Rule for the whole folder: **write your answer first, then open `_answers/`**. Sealed files are for checking work, not for reading ahead.

## What this app is and why it's in the portfolio
NetDropbox is Dropbox-style file storage. It has a .NET 8 API (`src/NetDropbox.Api`), MediatR handlers and services (`src/NetDropbox.Application`), entities (`src/NetDropbox.Domain`), EF Core + Postgres + blob storage + two background workers (`src/NetDropbox.Infrastructure`), and a React SPA (`web/src/App.tsx`, `web/src/api.ts`).
It's in the portfolio because one upload touches three stores with no shared transaction: the chunk folder, the blob store and Postgres (`CompleteUpload.cs:45-84`). That's the richest consistency, authorization and accounting material in the B01 .NET group.

## Known state of main (read before you try to run anything)
- **`main` does not build from a clean clone.** `.gitignore:8` (`storage/`) also matches `src/NetDropbox.Infrastructure/Storage/`, so the storage adapters were never committed (batch1report, netdrop §5).
- **The EF model throws on first use.** `NetDropboxDbContext.cs:44` maps `"_versions"` while `:48` maps the `Versions` navigation to the same field (verified by execution in B01 with a scratch console).
- **Ladder J0 fixes both** (`training/ladder/J0-make-main-build.md`). Incident branches carry an extra commit that vendors the adapters, so they build (all 5 verified in B01 with SDK 10.0.302 targeting net8.0).
- The test project (`tests/NetDropbox.Tests/NetDropbox.Tests.csproj`) references only Domain and Application, not Infrastructure. The 6 domain tests *should* therefore build on `main` without J0. This pass did not run them, so treat that as **UNVERIFIED**. B01 ran them 6/6 on the INC-001 branch with `DOTNET_ROLL_FORWARD=Major`.
- Your working tree has uncommitted Astra upload-integrity changes (15 files). Never switch branches in it. Use `git worktree` for everything (see `training/incidents/README.md`).

## Prerequisites
You need these first. Each points at a central page or at a file in this repo to read.
1. **C# classes, private setters, and exceptions as control flow.** Read `src/NetDropbox.Domain/UserStorageAccount.cs` and `src/NetDropbox.Domain/FileNode.cs` until you can say what every method guards. On-ramp exercises 1 and 3 check this.
2. **The ASP.NET request pipeline: controller → service/handler → middleware → HTTP status.** Read `src/NetDropbox.Api/Program.cs:76-96` and `src/NetDropbox.Api/Support/ExceptionMiddleware.cs`. On-ramp exercise 2 checks this.
3. **EF Core basics: DbContext, SaveChanges as the unit of work, indexes and FKs in `OnModelCreating`.** Read `src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs`. Then read [10 query plans and indexes](../../opusorganize/apprenticeship/curriculum/10-query-plans-and-indexes.md) (sections on N+1 and projections).
4. **Check-then-act races and why an in-memory check isn't a lock.** [03 concurrency races](../../opusorganize/apprenticeship/curriculum/03-concurrency-races.md).
5. **Dual writes and the outbox (what happens between "DB committed" and "queue got the message").** [01 transactional outbox](../../opusorganize/apprenticeship/curriculum/01-transactional-outbox.md) and [02 idempotency](../../opusorganize/apprenticeship/curriculum/02-idempotency.md).
6. **Verifying code you didn't write.** [05 verify before trust](../../opusorganize/apprenticeship/curriculum/05-verify-before-trust.md). This repo is its best example: a 47-file curriculum in `docs/upskill/` describes an app that doesn't compile.

Optional warm-up repos, earlier in the B01 chain: `Desktop\.netramp\learn\` (smallest app, characterization tests) and `Desktop\netauth\learn\` (JWT and auth basics that INC-005 builds on).

## Study route
Eleven sessions, about 13 hours in total, before the long mid/senior ladder. Every session ends with an exit criterion you can check. "Do by hand" means no agent: this is the thinking you're here to learn. "Delegate, then verify" means an agent may type, but you must check its work against the named evidence.

### Session 1 — Read the shape of the app (60 min)
- **Files/branches:** `README.md`, `src/NetDropbox.Api/Program.cs`, `src/NetDropbox.Infrastructure/DependencyInjection.cs`, `training/navigation/ARCHITECTURE.md` (read only up to "Check yourself").
- **Do by hand:** draw the four projects and the arrows between them on paper. List the 10 tables from `NetDropboxDbContext.cs:9-18`. Then do on-ramp exercises 1–2 (`learn/00-onramp.md`).
- **Delegate, then verify:** ask an agent to "list every HTTP route in this repo with its controller method". Check 3 of them by opening `src/NetDropbox.Api/Controllers/*.cs` yourself. Note anything the agent invented.
- **Exit criterion:** you can answer the "Check yourself" questions at the end of `training/navigation/ARCHITECTURE.md` without scrolling up, then compare with `training/_answers/navigation-architecture.md`.

### Session 2 — Domain rules and the schema (60 min)
- **Files/branches:** `src/NetDropbox.Domain/*.cs`, `NetDropboxDbContext.cs`, `src/NetDropbox.Domain/Common/Entity.cs`.
- **Do by hand:** on-ramp exercises 3–4. Write down, in one sentence each, which invariants the *database* enforces (unique indexes, the one FK) and which only the C# code enforces.
- **Delegate, then verify:** none. This session is reading practice.
- **Exit criterion:** you can explain without notes why `b.Property(x => x.Version).IsConcurrencyToken()` (`NetDropboxDbContext.cs:32,43`) protects nothing, using `Entity.cs:8` as evidence.

### Session 3 — The upload flow, end to end (75 min)
- **Files/branches:** `web/src/App.tsx:270-291`, `web/src/api.ts:130-164`, `src/NetDropbox.Application/Uploads/StartUpload.cs`, `UploadChunk.cs`, `CompleteUpload.cs`, `src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs`.
- **Do by hand:** on-ramp exercises 5, 7 and 8. Draw the sequence diagram browser → API → chunk folder → blob store → Postgres → Channel → worker, and mark which arrows are inside the one `SaveChangesAsync` (`CompleteUpload.cs:82`).
- **Delegate, then verify:** ask an agent to summarize "what happens if the API process dies right after `CompleteUpload.cs:82`". Check its answer against `ChannelProcessingQueue.cs` (in-memory) and the fact that nothing reads `processing_jobs` back (`grep -rn "ProcessingJobs" src`).
- **Exit criterion:** you can list the side effects of `CompleteUploadHandler.Handle` in order and say which ones survive a crash, then compare with `learn/_answers/00-onramp.md` #5.

### Session 4 — Your first test, and J0 (90 min)
- **Files/branches:** `tests/NetDropbox.Tests/Domain/*.cs`, `training/ladder/J0-make-main-build.md`. Work in a worktree: `git worktree add ../netdrop-j0 -b training/ladder/j0 main`.
- **Do by hand:** on-ramp exercise 6 (one characterization test). Then answer J0's four "Explain before touching" questions in writing *before* any edit.
- **Delegate, then verify:** let an agent make the J0 diff. Verify it yourself: `git check-ignore -v src/NetDropbox.Infrastructure/Storage/LocalBlobStorage.cs` prints nothing, the build passes **in a fresh clone** (not the agent's tree), and the model-builds test exists and fails when you revert the `NetDropboxDbContext.cs` change.
- **Exit criterion:** `dotnet build src/NetDropbox.Api/NetDropbox.Api.csproj` passes on a clean clone of your branch, and your characterization test passes. Compare with `training/_answers/ladder-J0.md` and `learn/_answers/00-onramp.md` #6.

### Session 5 — Junior tickets J1 and J4 (90 min)
- **Files/branches:** `training/ladder/J1-upload-status-404.md`, `training/ladder/J4-web-upload-client.md` (added in this pass, frontend).
- **Do by hand:** the "Explain before touching" answers for both tickets. For J4, reproduce the problem by reading, not running: trace a 401 through `web/src/api.ts:67-82`.
- **Delegate, then verify:** the J1 handler and tests. Check them against `training/agentic/VERIFY_CHECKLIST.md` "Read the diff" and "Fake or placeholder behavior".
- **Exit criterion:** the J1 HTTP-level test asserts `404` and `application/problem+json`, and it goes red when you put `SingleAsync` back. Your J4 written answers match `training/_answers/ladder-J4.md` on at least 3 of 4 defects.

### Session 6 — Blind review (90 min)
- **Files/branches:** `training/review/EXERCISE.md`, then `training/review/COMPARE.md`.
- **Do by hand:** all of it. A review is judgment, and delegating it defeats the exercise. Timebox 60 minutes, then 30 minutes to compare against `training/_answers/review.md`.
- **Delegate, then verify:** after you've scored yourself, ask an agent to review the same code independently. Compare its list with yours and the sealed list. Count its false positives: that number is your reason to verify agents.
- **Exit criterion:** you found at least 8 of the top 15 sealed findings, and for each miss you wrote the *pattern* you didn't look for (COMPARE.md format).

### Session 7 — Incidents 004 and 003 (90 min)
- **Files/branches:** `training/incidents/004-slow-folder-listing.md` and `003-shared-download-exposure.md`; branches `training/incidents/004-slow-folder-listing` and `training/incidents/003-shared-download-exposure` (open them in worktrees).
- **Do by hand:** hypothesis list before opening the diff; diagnosis; postmortem. Read [10 query plans](../../opusorganize/apprenticeship/curriculum/10-query-plans-and-indexes.md) for 004.
- **Delegate, then verify:** the regression test only (query-count test for 004, authorization-matrix test for 003). Use drill A and drill B in `learn/01-agentic-practice.md`.
- **Exit criterion:** each regression test fails on the incident branch and passes after your fix. Your postmortem names the root cause stated in `training/_answers/incident-00N.md`.

### Session 8 — Incident 001 and ladder M1 via the agentic exercise (90 min, then continue)
- **Files/branches:** `training/incidents/001-quota-meter-drift.md`, branch `training/incidents/001-quota-meter-drift`, `training/agentic/EXERCISE.md`.
- **Do by hand:** INC-001 diagnosis. Then Step 1 of the agentic exercise: the M1 SPEC.md with its failure matrix. Read [02 idempotency](../../opusorganize/apprenticeship/curriculum/02-idempotency.md) and [03 concurrency](../../opusorganize/apprenticeship/curriculum/03-concurrency-races.md) first.
- **Delegate, then verify:** Steps 2–5 of `training/agentic/EXERCISE.md` (implementer, adversarial reviewer, revision, `VERIFY_CHECKLIST.md`).
- **Exit criterion:** a concurrency test that ran against Postgres (Testcontainers start lines in the output) or an explicit "not executed" note, plus an EXPLAIN.md scored with `training/_answers/agentic-rubric.md`.

### Session 9 — Incidents 002 and 005 (90 min)
- **Files/branches:** `training/incidents/002-scan-backlog.md`, `005-staging-token-anomaly.md` and their branches.
- **Do by hand:** diagnosis and postmortems. For 005, trace config layering yourself. Read [14 load and back-pressure](../../opusorganize/apprenticeship/curriculum/14-load-backpressure-timeouts.md) for 002.
- **Delegate, then verify:** drill C in `learn/01-agentic-practice.md` (fail-closed config for 005).
- **Exit criterion:** you can explain without notes why "restart the pod" makes INC-002 worse, and why INC-005's alert self-resolving was the dangerous part.

### Session 10 — Mid ladder M3, M4, M2 (spread over several sittings)
- **Files/branches:** `training/ladder/M3-scan-follows-bytes.md`, `M4-storage-accounting.md`, `M2-search-at-scale.md`.
- **Do by hand:** every "Explain before touching" block, and the decision about which mechanism to use.
- **Delegate, then verify:** implementation. Use drill D in `learn/01-agentic-practice.md` for M3 (a security invariant).
- **Exit criterion:** for each ticket, one test that fails when you revert the production change, and a written answer compared with `training/_answers/ladder-M*.md`.

### Session 11 — Trade-offs, senior designs and interview kit (90 min per sitting)
- **Files/branches:** `training/navigation/TRADEOFFS.md`, `CHANGE_REQUEST.md`, `training/ladder/S1-S3`, `training/interview/*`.
- **Do by hand:** all of it. Designs and interview answers are your words or they're worthless in the room.
- **Delegate, then verify:** use an agent as the hostile interviewer in `training/interview/MOCK_DEFENSE.md`, never as the answer writer.
- **Exit criterion:** you score ≥ 12/18 on the MOCK_DEFENSE rubric, and you can tell two STAR stories from `PROJECT_STORIES.md` using your own incident notes.

## How you know you're done here
- All on-ramp answers match `learn/_answers/00-onramp.md` after at most one revision.
- J0, J1, J4, M1 are implemented on your own branches with tests that fail when the fix is reverted.
- Five incident postmortems are written before opening their answers, and at least 4 name the sealed root cause.
- Blind review found at least 8 of the top 15 findings.
- You can explain, without notes, the three things this repo teaches best: dual writes (`CompleteUpload.cs:82-84`), owner predicates as the only access control (`FileService.cs:143-147`, INC-003), and inert concurrency tokens (`Entity.cs:8`).

## Next repo
**netintercom** (`Desktop\netintercom\learn\`). It adds realtime (SignalR), multi-replica background jobs and a second frontend, on the same Api/Application/Domain/Infrastructure shape you just learned. B01 chain: .netramp → .netstripe → netauth → **netdrop** → netintercom → modular-monolith-with-ddd → eShop → gitvg/atlas (B02).
