# Trace Tables

## Trace 1: Upload

| Step | File/line | Value shape | Owner | Transformation | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | [uploadOne](../../../web/src/App.tsx#L270-L290) | `File` -> hashes/chunks | Browser | Splits file and uploads chunks | Memory pressure |
| 2 | [StartUploadHandler](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L78) | command -> session | Application | Validates quota/folder/idempotency | Quota race |
| 3 | [UploadChunkHandler](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L40-L55) | stream -> chunk row | Application/Infra | Stores and records chunk | Disk/DB mismatch |
| 4 | [CompleteUploadHandler](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L45-L84) | chunks -> blob/version/job | Application | Assembles, verifies, saves | No outbox |

## Trace 2: Authenticated Download

| Step | File/line | Value shape | Owner | Transformation | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) | route id | API | Extracts user id and streams result | Auth claim dependency |
| 2 | [OpenDownloadAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L114-L122) | node/version/blob | Application | Owner-scoped lookup | Version include growth |
| 3 | [EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155) | scan status | Application | Rejects unsafe states | Missing tests |

## Trace 3: Public Share

| Step | File/line | Value shape | Owner | Transformation | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17) | token/password | API | Anonymous request | Query-string password |
| 2 | [OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L55-L68) | share -> stream | Application | Checks permission and scan | Coupled analytics write |

## Trace 4: React Preview Error

| Step | File/line | Value shape | Owner | Transformation | Risk |
| --- | --- | --- | --- | --- | --- |
| 1 | [PreviewPanel effect](../../../web/src/App.tsx#L221-L238) | selected node | Browser | Downloads blob | No URL cleanup |
| 2 | [api.downloadBlob](../../../web/src/api.ts#L149-L157) | HTTP response | Browser | Throws on not OK | Error message loses problem body |

