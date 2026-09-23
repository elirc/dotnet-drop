# Review comparison — fill in after reading `_answers/review.md`

## Score
| | Count |
|---|---|
| My findings | |
| Model findings | |
| Matched (same root issue, even if worded differently) | |
| Mine that the model doesn't have (check them: real or false positive?) | |
| Critical/High I missed | |

## What I missed
| Model finding # | Severity | Why I missed it (didn't read that file / read it but didn't see the failure / didn't know the concept) |
|---|---|---|
| | | |

## Pattern behind the misses
Tick the ones that apply and write one sentence of evidence each:
- [ ] I reviewed code but not **build/config** (`.gitignore`, CI, compose, appsettings).
- [ ] I read happy paths and didn't ask "what if this runs **twice / concurrently**".
- [ ] I didn't follow **state across components** (DB ↔ blob store ↔ in-memory queue).
- [ ] I trusted **names** (`IsConcurrencyToken`, `Reserve`, `RemoveReference`) without checking that they're used or effective.
- [ ] I didn't check **authorization** on every read path (download by version, public share, copy).
- [ ] I believed the **README**.
- [ ] Other:

## One habit I'll change in the next review
> 

## Re-review
After a week, repeat the review on `training/incidents/003-shared-download-exposure` (the PR commit only). Did the habit fire?
