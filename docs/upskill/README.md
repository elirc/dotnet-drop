# NetDropbox Training Lab

Pinned snapshot: `UNCOMMITTED-NO-HEAD`. This project is not attached to a real commit yet; `git rev-parse HEAD` failed because the current branch has no commits. All code anchors in this suite refer to the working tree as inspected during this documentation pass.

NetDropbox is a synthetic training-sized cloud storage app: ASP.NET Core Web API, layered C# projects, EF Core/PostgreSQL persistence, background processing, local/S3-like blob storage, and a React/Vite client. The README advertises chunked upload, dedupe, share links, direct shares, scan-gated downloads, Docker Compose, and CI [README](../../README.md#L3-L16). The implementation has enough real moving parts to teach architecture, but the provenance check shows no organic history, no issue conventions, and very uniform code. Treat modules 06 and 07 as a simulated OSS workflow: the learner role-plays contributor, and the docs role-play maintainer expectations.

Use this curriculum in four modes:

| Learner | Path |
| --- | --- |
| Brand-new junior | Start with [00-fast-track](00-fast-track.md), then [system map](01-codebase-cartography/01-system-map.md), then two annotation drills. |
| Junior with stack familiarity | Trace upload and download in [key flows](01-codebase-cartography/05-key-flows.md), then attempt two good-first tickets. |
| Mid-level new to repo | Read [pattern catalog](03-architecture-and-patterns/05-pattern-catalog.md), [comparisons](03-architecture-and-patterns/07-comparative-implementations.md), and [systematic debugging](05-quality-engineering/03-systematic-debugging.md). |
| Senior doing review | Go straight to [architecture critique](03-architecture-and-patterns/06-architecture-critique.md), [risk register](08-reference/risk-register.md), and [comparisons](03-architecture-and-patterns/07-comparative-implementations.md). |

Schedule options:

| Timebox | Outcome |
| --- | --- |
| Weekend | Run verified commands, trace upload, explain one risk, make one tiny docs-only or test-only change. |
| Two weeks | Complete all flow traces, three drills, three tickets, one debugging scenario, one comparison migration plan. |
| Eight weeks | Add tests, propose a schema migration, write an RFC, review a fake PR, and defend design choices in interview-prep prompts. |
| Ongoing | Use the risk register as a backlog and repeat the senior critique every few features. |

Conventions:

- Anchors use `SymbolName` plus file and line range, for example [CompleteUploadHandler.Handle](../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86).
- Invented snippets are labeled `// Illustrative: not from this repo`.
- Verified commands have real output in [verification-log](08-reference/verification-log.md). Anything not run is labeled inferred.
- Drills are self-graded as Basic, Solid, or Strong.
- Junior asks: "How do I make it work?" Mid-level asks: "Is this the right pattern?" Senior asks: "What does this commit us to, who pays, and how do we reduce risk?"

**Verification Notes**

This suite is documentation-only. Product code was not intentionally changed during this pass. The project tier is Medium: five .NET projects plus a React client, but under 10k source LOC.

