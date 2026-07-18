# Verification Log

## Snapshot

- Pinned snapshot: `UNCOMMITTED-NO-HEAD`.
- `git rev-parse HEAD` output:

```text
HEAD
fatal: ambiguous argument 'HEAD': unknown revision or path not in the working tree.
```

The repository parent has a Git worktree, but this project has no committed HEAD. All anchors refer to the working tree inspected during this pass.

## Provenance Verdict

Verdict: **synthetic / freshly generated**.

Evidence:

- `git log --oneline -30`:

```text
fatal: your current branch 'main' does not have any commits yet
```

- No real CONTRIBUTING, SECURITY, CHANGELOG, LICENSE, or issue templates found by `rg --files -g ...`; only `README.md` matched.
- Dependency versions are uniformly modern and project style is highly regular.
- The repo was generated during the prior build session, so modules 06 and 07 are framed as simulated OSS workflow.

Answer-key exclusion: no `.seeded-flaws.md`, `ANSWER-KEY.md`, or similar answer-key file was found under this project by the answer-key search command.

## Commands Run

### Inventory

`rg --files` returned project files including API, Application, Domain, Infrastructure, React client, tests, Docker, CI, and this docs tree. Trimmed examples:

```text
README.md
docker-compose.yml
Directory.Build.props
web\src\App.tsx
src\NetDropbox.Api\Program.cs
src\NetDropbox.Application\Uploads\CompleteUpload.cs
src\NetDropbox.Infrastructure\Persistence\NetDropboxDbContext.cs
tests\NetDropbox.Tests\Domain\FileNodeTests.cs
```

### Backend Build

Command:

```powershell
& "$HOME\.dotnet\dotnet.exe" build src/NetDropbox.Api/NetDropbox.Api.csproj --no-restore
```

Output:

```text
NetDropbox.Domain -> ...\NetDropbox.Domain.dll
NetDropbox.Application -> ...\NetDropbox.Application.dll
NetDropbox.Infrastructure -> ...\NetDropbox.Infrastructure.dll
NetDropbox.Api -> ...\NetDropbox.Api.dll
Build succeeded.
0 Warning(s)
0 Error(s)
```

### Backend Tests

Command:

```powershell
& "$HOME\.dotnet\dotnet.exe" test tests/NetDropbox.Tests/NetDropbox.Tests.csproj --no-restore
```

Output:

```text
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 59 ms - NetDropbox.Tests.dll (net8.0)
```

### Frontend Build

Command:

```powershell
npm run build
```

from `web/`.

Output:

```text
vite v6.4.3 building for production...
✓ 1626 modules transformed.
✓ built in 17.71s
```

### Vulnerability Check

Command:

```powershell
& "$HOME\.dotnet\dotnet.exe" list src/NetDropbox.Api/NetDropbox.Api.csproj package --vulnerable --include-transitive
```

Output:

```text
The given project `NetDropbox.Api` has no vulnerable packages given the current sources.
```

## Opened Files Used For Anchors

Opened with line numbers in this session: API `Program`, upload/files/shares/auth/public controllers, exception middleware, application upload handlers, file/share services, domain `FileNode`/`UploadSession`, EF context, DI, chunk storage, queue, worker, janitor, seeder, React `App`, React `api`, test file, csproj/package/compose/CI/README.

## Self-Audit

### Link And Anchor Check

Command:

```powershell
# Parses every Markdown link under docs/upskill, resolves relative paths,
# and verifies #Lx-Ly anchors against physical line counts.
```

Output:

```text
Checked links: 363
All relative links and line anchors resolved.
```

### Random Anchor Sample

Ten randomly selected anchors were reopened and checked against target file line counts:

```text
OK ../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L30 lines=115
OK ../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L19-L21 lines=42
OK ../../../src/NetDropbox.Application/Files/FileService.cs#L13-L31 lines=157
OK ../../../src/NetDropbox.Domain/FileNode.cs#L51-L62 lines=114
OK ../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59 lines=70
OK ../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59 lines=70
OK ../../../src/NetDropbox.Application/Files/FileService.cs#L17-L31 lines=157
OK ../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L48 lines=88
OK ../../../src/NetDropbox.Api/Program.cs#L60-L70 lines=97
OK ../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59 lines=70
```

### Stub Scan

Output:

```text
Unfilled stub markers: 0
```

## Uncertainties

- Docker Compose was not run in this pass; `docker compose up --build` is inferred from README and compose files.
- No real HEAD commit exists, so anchors are pinned to the inspected working tree rather than an immutable commit.

## Unfilled Stubs

None.
