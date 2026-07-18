# NetDropbox

Personal/team cloud storage built with .NET 8 Web API, EF Core, PostgreSQL, React, TypeScript, Vite, and TanStack Query.

## What Is Included

- Clean/layered backend projects: `Api -> Application -> Domain -> Infrastructure`.
- Versioned REST API under `/api/v1` with Swagger, JWT auth, RFC 7807-style problem responses, health checks, rate limiting, correlation IDs, and Serilog.
- Nested files/folders, trash/restore, rename, move, copy, breadcrumb-ready listing, search, previews, versions, direct shares, and link shares.
- Chunked uploads with SHA-256 per-chunk checksums, resumable upload sessions, parallel client uploads, server-side assembly, and final content hash verification.
- Content-addressed blob storage with deduplication and reference counts.
- Local disk blob storage plus S3-compatible MinIO implementation.
- Background processing pipeline with queued scan jobs, retry/dead-letter state, simulated EICAR detection, text extraction, image thumbnail generation, quarantine, and janitor cleanup.
- Quota checks at upload-session start and a usage meter in the client.
- Docker Compose for PostgreSQL, Redis, RabbitMQ, MinIO, and the API.
- GitHub Actions CI for backend build/tests and frontend build.

## Seed Login

The development seeder creates:

- `owner@netdropbox.local` / `Passw0rd!`
- `teammate@netdropbox.local` / `Passw0rd!`

## Run Locally

Start infrastructure and API:

```bash
docker compose up --build
```

Start the web client:

```bash
cd web
npm install
npm run dev
```

Open `http://localhost:5173`.

Swagger is available at `http://localhost:5080/swagger`, and health checks at `http://localhost:5080/healthz`.

## Storage Providers

Local disk is the default:

```text
Storage__Provider=Local
Storage__LocalRoot=storage
```

For MinIO/S3-compatible storage:

```text
Storage__Provider=S3
Storage__S3__ServiceUrl=http://localhost:9000
Storage__S3__AccessKey=netdropbox
Storage__S3__SecretKey=netdropbox-secret
Storage__S3__Bucket=netdropbox
```

## Upload Flow

1. Client hashes the whole file and calls `POST /api/v1/uploads`.
2. Server returns an idempotent session plus already-uploaded chunk indexes.
3. Client uploads missing chunks in parallel using `PUT /api/v1/uploads/{sessionId}/chunks/{index}` with `X-Chunk-SHA256`.
4. Client calls `POST /api/v1/uploads/{sessionId}/complete`.
5. Server assembles chunks, verifies final SHA-256, deduplicates the blob, creates a new file version, and queues processing.
6. Downloads are blocked until scan status is `Clean`; infected files are quarantined.

## Tests

```bash
dotnet test tests/NetDropbox.Tests/NetDropbox.Tests.csproj
```

Frontend:

```bash
cd web
npm run build
```

## Notes

The development seeder uses `EnsureCreatedAsync` so Docker/local first-run creates tables immediately. For production-style schema evolution, add an initial EF migration and switch startup to `MigrateAsync`.
