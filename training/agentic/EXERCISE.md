# Agentic exercise — ship ladder M1 (idempotent upload completion) with agents

You direct; agents type. You own the spec, the review decision and the final explanation. Budget: about 4 hours. Do J0 first in the same worktree.

## Step 0 — Set up (you)
`git worktree add ../netdrop-m1 -b training/agentic/m1-idempotent-complete main`, then apply your J0 fix. Confirm `dotnet build` passes and the six existing tests pass.

## Step 1 — Spec and design note (you, no agent, 45 min)
Write `training/agentic/m1/SPEC.md` (≤ 1 page):
- The problem, in terms of lines: `CompleteUpload.cs:33` (the guard) and `:82-84` (the effects after commit).
- The chosen mechanism (claim-by-update with a lease, `FOR UPDATE`, or `xmin`) and **why not the others**.
- The exact API contract for: a first call, a sequential retry, a concurrent loser, a crashed claimer.
- The failure matrix (crash after blob / after commit / after chunk delete / after enqueue).
- The tests that prove it, by name, including one **Testcontainers Postgres** concurrency test.
- Out of scope: the outbox (S1) and the quota model (INC-001).

## Step 2 — Implementer agent
Prompt (fill in the blank; don't let the agent choose the mechanism):
> You are implementing SPEC.md in this repo. Mechanism: ____. Touch only: `CompleteUpload.cs`, `UploadSession.cs`, `Enums.cs`, `NetDropboxDbContext.cs`, and new tests under `tests/NetDropbox.Tests/Uploads/`. Do not change public DTOs except to add fields. Do not add packages other than `Testcontainers.PostgreSql` (already referenced), `Microsoft.EntityFrameworkCore.InMemory` and `Npgsql.EntityFrameworkCore.PostgreSQL` for tests. Run `dotnet build` and `dotnet test` and paste the real output. If a test can't run (no Docker), say so; don't mark it skipped silently.

## Step 3 — Adversarial reviewer agent (a different model if available)
> Review this diff against SPEC.md. Only report: correctness, concurrency, failure handling, security, performance, test adequacy, architecture. No style. For each finding: `file:line`, the concrete interleaving or input that breaks it, and severity. Specifically check: (1) can two callers both reach `AddVersion`? (2) what happens if the claimer dies: does the lease ever expire, and who takes over? (3) is the second call's response identical to the first? (4) does any test actually run concurrently against Postgres, or is it sequential on InMemory? (5) is `Version`/`xmin` now effective, or still inert?

## Step 4 — Revision
Feed the findings back to the implementer. **You** decide which findings to accept, and write one line per rejected finding with the reason.

## Step 5 — Verification (you)
Work through [VERIFY_CHECKLIST.md](VERIFY_CHECKLIST.md) line by line. Attach the evidence (command output) in `training/agentic/m1/VERIFY.md`.

## Step 6 — Your explanation, graded
Write `training/agentic/m1/EXPLAIN.md` (≤ 1 page, no code blocks longer than 5 lines):
- What was broken, as a timeline of two requests.
- What the fix does, as the same timeline.
- One thing the agent got wrong that you caught, and how you caught it.
- One thing still not safe after this PR.

Then give EXPLAIN.md to the reviewer agent with the rubric in `training/_answers/agentic-rubric.md` and ask for a score plus the weakest sentence.
