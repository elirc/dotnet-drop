# NetDropbox apprenticeship lab (B01, tier LAB)

NetDropbox is a .NET 8 Web API (Api / Application / Domain / Infrastructure), a React + TypeScript SPA, Postgres, a local or S3 blob store, and an in-process background worker. It's the richest distributed-consistency material in the archive: one upload touches three stores with no shared transaction.

**Base commit:** `main` @ `d612e0b`. All exercises target the *committed* code at that commit. *(Status 2026-10-06: the Astra upload-integrity changes this line once called "uncommitted" are now committed on `main` as `9d28a29`. Exercises still target `d612e0b`; use `git worktree` for everything.)*

> **Warning:** `main` as committed doesn't build (the storage adapters are git-ignored) and, at `d612e0b`, its EF model throws at startup. That's review findings #1–#2 and ladder **J0**. *(Status 2026-10-06: the EF mapping is fixed on current `main` by `9d28a29`; the adapters are still missing.)* The incident branches were meant to carry a commit that adds the adapters.
>
> **Status (2026-10-06): the incident branches were never pushed.** `git branch -r` on a fresh clone lists only `origin/main`, so none of `training/incidents/NNN-*` exists remotely, and neither do their adapter commits. The incident briefs, review, ladder, navigation and interview material are all still usable. The five incidents can be read and reasoned about, but can't be bisected or reproduced. Details: `incidents/README.md`.

## Suggested order (about 50 hours total)

| # | What | Where | Time |
|---|---|---|---|
| 1 | Blind review of `main`, 60 min timebox, then compare | [review/EXERCISE.md](review/EXERCISE.md) → [review/COMPARE.md](review/COMPARE.md) | 2.5 h |
| 2 | Architecture reconstruction; answer the self-check | [navigation/ARCHITECTURE.md](navigation/ARCHITECTURE.md), [navigation/FIRST_CHANGE.md](navigation/FIRST_CHANGE.md) | 1.5 h |
| 3 | Ladder J0 → J1 → J3 → J2 | [ladder/](ladder/README.md) | 8.5 h |
| 4 | Incidents 004, 003, 001 (DB, authz, accounting) | [incidents/](incidents/README.md) | 4.5 h |
| 5 | Ladder M1 → M3 → M4 → M2 | [ladder/](ladder/README.md) | 21 h |
| 6 | Incidents 002, 005 (async, config) | [incidents/](incidents/README.md) | 3 h |
| 7 | Trade-off drills plus change request CR-14 | [navigation/TRADEOFFS.md](navigation/TRADEOFFS.md), [navigation/CHANGE_REQUEST.md](navigation/CHANGE_REQUEST.md) | 2.5 h |
| 8 | Agentic exercise (ships M1 with an implementer and a reviewer agent) | [agentic/EXERCISE.md](agentic/EXERCISE.md) | 4 h |
| 9 | Senior designs S1 → S2 → S3 | [ladder/](ladder/README.md) | 9 h (design only) |
| 10 | Interview kit: stories, 20 questions, system design, mock defense, flashcards | [interview/](interview/) | 5 h |

If you only have one weekend: do 1, 3 (J0/J1), 4 (003 only), and M1 via the agentic exercise.

## Layout
- `ladder/`: 11 tickets (J0 setup, 3 junior, 4 mid, 3 senior), each gated by "Explain before touching".
- `review/`: the blind review exercise and the comparison template.
- `incidents/`: 5 on-call briefs. The bugs live on `training/incidents/NNN-*` branches.
- `navigation/`: architecture with evidence, first-change onboarding, 5 trade-off drills, one change request.
- `agentic/`: a scripted spec → implement → adversarial review → verify → explain workflow, plus an AI-output checklist.
- `interview/`: STAR scaffolds, 20 questions, a system-design prompt, a mock-defense script, 25 flashcards.
- `_answers/`: **sealed**. One file per exercise. Open one only after you've written your own answer.

## Existing learning material in this repo
- `docs/upskill/` (47 files): a broad reading curriculum with a good risk register. Use it for background. It misses review findings #1–#5.
- `astraupskill/`: a worked change for upload integrity that matches the uncommitted diffs. Read it after ladder M1 as a second opinion.
