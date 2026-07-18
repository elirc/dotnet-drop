# Learning Rubrics

## Junior

- Can run verified commands.
- Can trace upload from client to handler.
- Can explain owner filter vs `[Authorize]`.
- Can add a focused domain test.

## Mid-Level

- Can design a cross-layer feature with API, application, persistence, UI, and tests.
- Can identify where EF query shape matters.
- Can debug a stuck processing job using queue and worker anchors.
- Can review for IDOR and scan-gate failures.

## Senior

- Can compare viable designs and name who pays for each tradeoff.
- Can propose migration and rollback for outbox/search/auth changes.
- Can distinguish confirmed bugs from hypotheses.
- Can turn risk register items into staged roadmap work.

Self-assessment:

| Prompt | Basic | Solid | Strong |
| --- | --- | --- | --- |
| Explain upload consistency | Names endpoints | Explains DB/blob/queue side effects | Designs outbox and fault tests |
| Review auth | Checks `[Authorize]` | Checks owner filters | Builds endpoint matrix and regression tests |
| Discuss performance | Mentions speed | Identifies query/allocation hotspots | Measures and sets budget |
| Interview design tradeoff | Picks favorite | Compares dimensions | Names switch signals and migration cost |

