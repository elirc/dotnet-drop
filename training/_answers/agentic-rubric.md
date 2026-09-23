# Rubric — grading EXPLAIN.md for M1 (for the reviewer agent)

Score each criterion 0–2. Total /12. Pass is 9 or more.

| # | Criterion | 0 | 1 | 2 |
|---|---|---|---|---|
| 1 | Broken-timeline accuracy | vague ("race condition") | names both requests but not where they diverge | names line `CompleteUpload.cs:33` as the unguarded check, and the effects both requests then perform (version, `AddReference`, `Reserve`) |
| 2 | Mechanism explanation | buzzwords ("made it atomic") | describes the mechanism | explains *why exactly one* caller wins (affected rows == 1, row lock, or xmin mismatch), and what the loser sees |
| 3 | Crash semantics | absent | mentions crashes | states what happens when the claimer dies and how the lease or rollback recovers |
| 4 | Contract | absent | says retries are OK | gives the concrete responses: sequential retry → 200 with the same body; concurrent loser → documented status/code |
| 5 | Agent error caught | none, or generic | a real error with no method | a real error, and the verification step that exposed it (e.g. the test ran on InMemory, or reverting the fix kept tests green) |
| 6 | Remaining risk | "none" | a generic risk | a specific one, e.g. enqueue after commit still loses work (S1), or the quota reservation model (INC-001) |

**Interviewer notes for the reviewer.** Deduct a point for any claim with no mechanism behind it. If the author says "transaction" without saying which statements are inside it, ask. Quote the weakest sentence back to the author.
