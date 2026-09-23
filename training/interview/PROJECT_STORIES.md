# NetDropbox STAR stories — complete these after doing the work

Only write a story once you've actually done the exercise it's based on. Keep each to about 2 minutes spoken. Fill in the brackets from your own notes. Don't claim production scale you didn't have; "in a training lab I…" is honest and still strong.

## 1. Debugging: "the build that only worked on my machine" (J0)
- **S:** A .NET 8 file-storage API whose CI was advertised as green; a fresh clone didn't compile.
- **T:** Make `main` reproducible without changing behavior.
- **A:** [how you found `.gitignore:8` swallowing `Infrastructure/Storage/` on a case-insensitive FS; the one git command that proved it; the second failure (EF model: `_versions` field mapped twice) and how you isolated it without a DB]
- **R:** [clean-clone build; the guard you added so an ignored `.cs` file fails CI]
- **Lesson:** "Ignored" and "missing" look the same in `git status`.

## 2. Trade-off: "where scan status should live" (M3/S2)
- **S:** A virus-scan status on the file node, while bytes are deduped per blob and shared across versions and users.
- **T:** Close two quarantine bypasses (copy, version download).
- **A:** [node vs version vs blob; why you shipped the small fix first and planned the data move with expand/contract]
- **R:** [tests for all four byte-serving paths; the migration plan's rollback points]

## 3. Incident: "backoff that took down the queue" (INC-002)
- **S:** After a "harmless" retry-backoff PR, scan latency went from 2 s to 38 min while CPU dropped.
- **T:** Diagnose and fix without losing queued work.
- **A:** [reading "CPU down, latency up" as a waiting problem; finding the `Task.Delay` in a single serial consumer; the HEIC poison messages; why restarting would have lost the in-memory queue]
- **R:** [the fix: not-before scheduling + error classification; the test with poison + good messages]

## 4. Migration: "adopting migrations on a hand-made prod schema" (S2)
- **S:** No EF migrations; schema created by `EnsureCreated` in dev only.
- **A:** [baseline + history seeding; migrations as a separate pipeline step; a 6-deploy expand/contract with a batched backfill]
- **R:** [what you verified; which step is the point of no return]

## 5. Agent-caught bug / bug I caught in the agent (agentic M1)
- **S:** An implementer agent produced an "idempotent complete" PR with green tests.
- **A:** [the adversarial review question that exposed it, e.g. the concurrency test ran sequentially on EF InMemory, or it relied on the inert `Version` token]
- **R:** [what changed; how you now verify agent claims (VERIFY_CHECKLIST)]
