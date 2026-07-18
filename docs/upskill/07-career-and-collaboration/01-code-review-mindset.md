# Code Review Mindset

Review in this order:

1. Does it work?
2. Is it correct under failure?
3. Will it stay correct?
4. Does it fit the repo's boundaries?
5. Is it kind to future maintainers?

Repo checklist:

- Controllers stay thin like [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59).
- Owner filters resemble [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146).
- Upload paths preserve final hash verification [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L48).
- Public links preserve token/password/permission/scan checks [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L53-L68).

