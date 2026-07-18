# Observability And Operations

## What Exists

- Serilog request logging is configured [Program](../../../src/NetDropbox.Api/Program.cs#L16-L19), [Program](../../../src/NetDropbox.Api/Program.cs#L88-L88).
- Correlation id middleware runs before exception handling [Program](../../../src/NetDropbox.Api/Program.cs#L78-L80).
- Health check endpoint maps `/healthz` [Program](../../../src/NetDropbox.Api/Program.cs#L93-L94).
- Worker logs infected uploads and job failures [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L47-L75).

## How Would I Know This Broke?

| Flow | Signal to add |
| --- | --- |
| Upload start | quota rejects, session creation count, duplicate idempotency keys |
| Chunk upload | checksum mismatch rate, average chunk write latency |
| Completion | final hash mismatch, orphaned chunks, blob save failures |
| Processing | queued/running/failed/dead-lettered job counts |
| Public share | invalid token rate, revoked-link attempts, password failures |
| Search | query duration and row count |

