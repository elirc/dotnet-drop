# Type System And Contracts

## Records As Boundary Contracts

Request and response shapes use records: [StartUploadRequest](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L17-L21), [CompleteUploadCommand](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L11-L18), and DTOs in [api.ts interfaces](../../../web/src/api.ts#L4-L48). Records are good for immutable messages; avoid placing behavior in them.

## Nullable References

Nullable is enabled [Directory.Build.props](../../../Directory.Build.props#L1-L9). Domain fields like `ParentId`, `CurrentVersionId`, `ThumbnailPath`, and `ExtractedText` model real absence [FileNode properties](../../../src/NetDropbox.Domain/FileNode.cs#L20-L30). Drill: explain why `Guid? ParentId` is better than `Guid.Empty`.

## Enums And Cross-Language Drift

Backend scan status is an enum [FileNode.ScanStatus](../../../src/NetDropbox.Domain/FileNode.cs#L23-L25). Frontend accepts both string and numeric enum forms [api.ts types](../../../web/src/api.ts#L1-L2). That is defensive, but it also masks contract drift. A senior would prefer generated OpenAPI types or a shared schema.

## Exceptions As Use-Case Errors

Application methods throw `AppException` for expected failures, for example quota and not found [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L48). Middleware maps exceptions into problem details [ExceptionMiddleware](../../../src/NetDropbox.Api/Support/ExceptionMiddleware.cs#L7-L29). Alternative: explicit result types; see comparisons.

