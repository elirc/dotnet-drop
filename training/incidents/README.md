# NetDropbox incidents

> **Status (checked 2026-10-06): none of the five branches below exists on the remote.** A fresh clone of `github.com/elirc/dotnet-drop` has only `origin/main`. The briefs are intact and the sealed answers in `training/_answers/incident-00N.md` describe each root cause, so you can still practice the *diagnosis* half: read the alert, logs and metrics, write a ranked hypothesis list, then predict the bad diff. But you can't `git diff` the suspect PR or run a failing regression test until someone recreates and pushes the branches. The `git -C ... worktree add` command below also uses an absolute path from the author's machine. Run `git worktree add ../netdrop-inc-001 <branch>` from your own clone instead, once branches exist.

Five on-call scenarios. Each one is a real bug committed on its own branch. The brief (`NNN-*.md`) shows what on-call would see: the alert, some logs, a metrics excerpt and the PR that shipped just before the symptom started. The cause is not stated anywhere outside `training/_answers/`.

| # | Branch | Brief |
|---|---|---|
| 001 | `training/incidents/001-quota-meter-drift` | [001-quota-meter-drift.md](001-quota-meter-drift.md) |
| 002 | `training/incidents/002-scan-backlog` | [002-scan-backlog.md](002-scan-backlog.md) |
| 003 | `training/incidents/003-shared-download-exposure` | [003-shared-download-exposure.md](003-shared-download-exposure.md) |
| 004 | `training/incidents/004-slow-folder-listing` | [004-slow-folder-listing.md](004-slow-folder-listing.md) |
| 005 | `training/incidents/005-staging-token-anomaly` | [005-staging-token-anomaly.md](005-staging-token-anomaly.md) |

## How to open an incident without touching your working tree

Your `main` working tree has uncommitted changes (the Astra upload-integrity work). Do not switch branches in it. Use a worktree instead:

```bash
git -C C:/Users/Owner/Desktop/netdrop worktree add ../netdrop-inc-001 training/incidents/001-quota-meter-drift
cd ../netdrop-inc-001
dotnet build src/NetDropbox.Api/NetDropbox.Api.csproj
# ...investigate...
git -C C:/Users/Owner/Desktop/netdrop worktree remove ../netdrop-inc-001
```

Every incident branch has two commits on top of `main`:
1. `chore: add Infrastructure/Storage adapters so this branch builds`. The three storage adapters are missing from `main`'s history (review exercise: find out why). This commit only adds them so the branch compiles.
2. The suspect PR. That's the one the brief quotes.

Builds were verified with the .NET 10 SDK targeting net8.0. To run the tests on a machine that has only the .NET 10 runtime, set `DOTNET_ROLL_FORWARD=Major`.

## Rules for yourself

1. Write your hypothesis list **before** you open the diff. Then rank the hypotheses by the evidence in the brief.
2. Reproduce the bug with a failing test before you write the fix.
3. Write the postmortem (timeline, impact, root cause, why the review missed it, action items), then compare it with the model answer.
4. Timebox: 60–90 minutes per incident.

Answers: `training/_answers/incident-NNN.md`. Open one only after your postmortem is written.
