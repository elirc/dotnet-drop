# Validation, Auth, And Permissions

## Authentication

JWT bearer auth is configured in [Program](../../../src/NetDropbox.Api/Program.cs#L41-L58). Login issues a token with user id, email, and display name claims [AuthController.Login](../../../src/NetDropbox.Api/Controllers/AuthController.cs#L30-L45). Controllers use `[Authorize]`, for example [FilesController](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L9-L12).

## Authorization

Resource authorization is mostly owner filtering in application services: [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146), [StartUploadHandler parent check](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L41-L49), and [ShareService.CreateLinkAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L11-L20). Junior mistake: checking only `[Authorize]` and forgetting owner filters. Senior check: every route accepting a resource id must prove ownership or token permission.

## Public Token Boundary

Share links use anonymous access and a token path [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17). Permission, password, expiry, and clean scan status are checked server-side [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68). Risk: password travels as query string.

## Validation

FluentValidation guards request shape [StartUploadValidator](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L20-L28), [UploadChunkValidator](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L17-L24). Domain guards still enforce invariants [FileNode.CleanName](../../../src/NetDropbox.Domain/FileNode.cs#L106-L112).

