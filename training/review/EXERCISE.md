# Blind code review — NetDropbox (60 minutes)

You're the new senior on the team. Before the first production launch, the lead asks for a review of **the committed code on `main`** (`git show HEAD:<path>`, or a clean worktree of `main`). Don't review the uncommitted Astra changes in the working tree. They're a separate PR.

## Scope
- Backend: `src/NetDropbox.Api`, `src/NetDropbox.Application`, `src/NetDropbox.Domain`, `src/NetDropbox.Infrastructure`
- Frontend: `web/src/App.tsx`, `web/src/api.ts`
- Build and ops: `.gitignore`, `.github/workflows/ci.yml`, `docker-compose.yml`, `appsettings.json`, `README.md`
- Tests: `tests/NetDropbox.Tests`

## Rules
1. Timebox 60 minutes. Stop even if you're not done. What you found in 60 minutes is the data point.
2. Each finding: **severity** (Critical / High / Medium / Low), **category** (correctness, security, data integrity, concurrency, failure handling, performance, operability, tests, maintainability), **evidence** (`file:line`), **why it matters in production** (one sentence, as a concrete failure a user or operator would see).
3. Mark the findings you think are **typical old-AI-generated smells** with `[AI]`: fake integrations, docs claiming things the code doesn't do, hardcoded fallbacks, missing transaction boundaries, tests that don't test the risky part.
4. Also answer these three questions:
   - Does `main` build from a fresh clone? How do you know without running it?
   - Which README claims are not true of the code?
   - What is the single change you'd block the launch on?

## Suggested path (not required)
Follow one file's life: upload start → chunk → complete → worker scan → download → share link → delete → janitor purge. At every step, ask what happens if this runs twice, concurrently, or crashes halfway.

## Deliverable
`training/review/MY_FINDINGS.md` (create it). Then fill in [COMPARE.md](COMPARE.md) against `training/_answers/review.md`.
