# Fake Code Contrasts

## Contrast 1: Missing Owner Filter

Resembles: [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146).

```csharp
// Illustrative: not from this repo
var node = await db.FileNodes.SingleAsync(x => x.Id == nodeId, ct);
```

Better:

```csharp
// Illustrative: not from this repo
var node = await db.FileNodes.SingleAsync(x => x.Id == nodeId && x.OwnerId == userId, ct);
```

Lesson: authentication is not authorization.

## Contrast 2: Buffering Download

Resembles: [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59).

```csharp
// Illustrative: not from this repo
var bytes = await stream.ReadAllBytesAsync();
return File(bytes, mimeType, fileName);
```

Better: stream the file result as the repo does. Lesson: large files punish buffering.

## Contrast 3: Side Effect Before Verification

Resembles: [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L42-L56).

```csharp
// Illustrative: not from this repo
await blobs.SaveAsync(hash, stream, ct);
if (hash != expected) throw;
```

Better: verify final content hash before saving the blob.

## Contrast 4: Unbounded Parallel Upload

Resembles: [uploadOne](../../../web/src/App.tsx#L285-L288).

```tsx
// Illustrative: not from this repo
await Promise.all(chunks.map(send));
```

Better: keep bounded worker count. Lesson: client concurrency is still load on your API.

