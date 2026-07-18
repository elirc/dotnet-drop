# Security Checklist

| Area | Check | Evidence |
| --- | --- | --- |
| Authentication | JWT validates issuer, audience, signing key, lifetime. | [Program JWT](../../../src/NetDropbox.Api/Program.cs#L41-L58) |
| Resource authorization | Every id route filters by owner or token permission. | [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146) |
| Public tokens | Token route checks expiry, password, permission, scan status. | [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68) |
| Rate limiting | Public share route has fixed-window limiter. | [Program rate limiter](../../../src/NetDropbox.Api/Program.cs#L60-L70) |
| Upload malware gate | Infected files become undownloadable. | [ProcessingWorker infected path](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L45-L52), [EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155) |
| Input validation | Commands validate file names, hash lengths, chunk size. | [StartUploadValidator](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L20-L28) |
| Path traversal | File/folder names reject separators. | [FileNode.CleanName](../../../src/NetDropbox.Domain/FileNode.cs#L106-L112) |
| Secrets | Compose contains local dev secrets. | [docker-compose env](../../../docker-compose.yml#L55-L68) |

Pre-merge checklist:

- Does every resource id prove ownership or token permission?
- Does any public endpoint need stricter rate limiting?
- Are upload bytes unavailable until scan passes?
- Are passwords or tokens placed in URLs?
- Did dependency vulnerability check pass?

