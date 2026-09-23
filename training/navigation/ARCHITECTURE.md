# NetDropbox architecture, reconstructed from code (`main` @ d612e0b)

Every claim cites evidence. `UNKNOWN` marks what the code can't tell you.

## System context
- **Clients:** a React 18 SPA (`web/src`, Vite dev server on :5173 proxying `/api` to :5080, `web/vite.config.ts`) and anonymous public-link downloaders (`PublicSharesController`).
- **API:** one ASP.NET Core 8 process (`src/NetDropbox.Api/Program.cs`). It hosts the HTTP API **and** two background services (`DependencyInjection.cs:55-56`).
- **Stores:** Postgres, schema `netdropbox` (`NetDropboxDbContext.cs:22`); a blob store, Local disk or S3/MinIO (`DependencyInjection.cs:33-53`); a chunk scratch area on local disk (`:30-31`).
- **Provisioned but unused:** Redis and RabbitMQ (`docker-compose.yml:18-30`; no package references or code).

## Components and dependency direction
```
Api (controllers, middleware) ──> Application (MediatR handlers, services, ports) ──> Domain (entities, invariants)
      └──────────────> Infrastructure (EF DbContext, storage adapters, workers) ──> Application ports
```
- Application defines ports: `IAppDbContext`, `IBlobStorage`, `IChunkStorage`, `IProcessingQueue`, `IClock`, `IPasswordHasher` (`Application/Abstractions/*`).
- **Leaks:** `IAppDbContext` exposes EF `DbSet<T>` (`IAppDbContext.cs:8-17`), so Application depends on EF Core (`NetDropbox.Application.csproj:8`). `UploadsController.Status` queries the DB from the API layer (`UploadsController.cs:23-29`). `AuthController` does persistence and token work inline (`AuthController.cs:22-46`).
- Two styles coexist: uploads use MediatR commands plus a FluentValidation pipeline (`Program.cs:28-30`, `ValidationBehavior.cs`), while files and shares use plain scoped services (`FileService`, `ShareService`). UNKNOWN why. It's probably generator drift.

## Three key flows
**1. Chunked upload.**
`App.tsx:270 uploadOne` → `sha256(file)` in the browser → `POST /api/v1/uploads` → `StartUploadHandler` (quota *check*, parent check, idempotent session lookup by owner+parent+name+hash, `StartUpload.cs:33-78`) → for each missing chunk, `PUT …/chunks/{i}` with `X-Chunk-SHA256` (4 in parallel, `App.tsx:285-288`) → `UploadChunkHandler` → `FileSystemChunkStorage.StoreAsync` writes `{root}/{session:N}/{i:D8}.chunk` and hashes while writing → checksum compare → `upload_chunks` row (unique on session+index) → `POST …/complete` → `CompleteUploadHandler`: verify all indexes present, assemble to a temp file, SHA-256 the whole file, dedupe on `blobs.content_hash`, save to the blob store if new, else `AddReference`; find a same-named file or create one; `AddVersion` (sets `PendingScan`); a `ProcessingJob` row; `session.Complete`; `user.Reserve`; **one `SaveChanges`**; delete chunks; **enqueue into the in-memory Channel** (`CompleteUpload.cs:82-84`).

**2. Processing.**
`ProcessingWorker.ExecuteAsync` reads the Channel serially → a scope per message → load job, node and version → `MarkRunning` and save → read the first 1 MB and look for the EICAR string → Infected (quarantine) or: extract text if text-like, thumbnail if `image/*` (ImageSharp, key `thumbnail-{nodeId}.png`) → `MarkScanClean` + a `ScanResult` + job Completed. On exception: job Failed or DeadLettered after 3 attempts, node `Failed`, immediate re-enqueue (`ProcessingWorker.cs:26-76`).

**3. Download and public share.**
Owner: `GET /files/{id}/download?versionId=` → `FileService.OpenDownloadAsync`: owner + not deleted → `EnsureDownloadable` (node-level scan status) → pick a version → blob → stream with range support (`FilesController.cs:55-60`).
Public: `GET /public/shares/{token}/download?password=` (rate-limited 60/min/IP, `Program.cs:60-71`) → `ShareService.OpenPublicDownloadAsync`: token lookup → `IsUsable` (not revoked, not expired) → PBKDF2 password verify → permission ≥ Download → node Clean → current version → `RecordAccess` and save → stream.

## Data model (`NetDropboxDbContext.cs`)
`users` (unique email; quota/used bytes) · `file_nodes` (tree via `parent_id`, **no FK**; soft delete; node-level `scan_status`; `extracted_text` text; `current_version_id`, **no FK**) · `file_versions` (the **only FK in the model**, → file_nodes, cascade; unique `(file_node_id, version_number)`) · `blobs` (unique `content_hash`, `ref_count`) · `upload_sessions` / `upload_chunks` (unique `(session, index)`) · `share_links` (unique token) · `direct_shares` (unique `(node, shared_with)`) · `scan_results` · `processing_jobs`. Enums are stored as strings. `Version` is a concurrency token on users and file_nodes, but it's a plain `bigint` that never changes (verified; review #10).

## Test strategy
Six domain tests (`tests/NetDropbox.Tests/Domain/*`). No handler, API, persistence, authorization or concurrency tests. `Testcontainers.PostgreSql` is referenced but unused. (The uncommitted Astra work adds handler tests on EF InMemory and filesystem tests.)

## Build and deploy
`Directory.Build.props` (net8.0, nullable) · `Dockerfile` (SDK build → aspnet runtime, :8080) · `docker-compose.yml` (API with `ASPNETCORE_ENVIRONMENT=Development`, so `EnsureCreated` and seeding run) · CI (`.github/workflows/ci.yml`: restore, build API, test; npm ci + build for web). On `main`, CI would fail: the storage adapters aren't committed (review #1). Deployment beyond compose: UNKNOWN, there are no manifests.

## Check yourself (answer without scrolling up)
1. Which single line decides whether a completed upload is ever scanned?
2. Name the two places where Application depends on infrastructure concerns.
3. Which table relationships are enforced by the database, and which only by code?
