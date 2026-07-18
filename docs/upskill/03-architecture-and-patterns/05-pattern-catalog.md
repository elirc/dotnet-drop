# Pattern Catalog

## Pattern: Thin Controller, Use-Case Handler

**Problem:** Keep HTTP concerns separate from business sequencing.

**Real example:** [UploadsController.Complete](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L38-L40) delegates to [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86).

**Why it works:** Controllers stay easy to scan; tests can target handlers.

**Failure modes:** Small exceptions like [UploadsController.Status](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L23-L29) can normalize query logic in controllers.

**Drill:** Move status into a query object on paper and list the tests you would preserve.

## Pattern: Domain State Transition

**Problem:** Prevent invalid state changes from spreading across services.

**Real example:** [FileNode.AddVersion](../../../src/NetDropbox.Domain/FileNode.cs#L51-L62), [FileNode.MarkInfected](../../../src/NetDropbox.Domain/FileNode.cs#L94-L98).

**Why it works:** Callers cannot forget to set `CurrentVersionId` or `ScanStatus`.

**Failure modes:** Domain uses `DateTimeOffset.UtcNow` directly for trash; time is harder to test.

## Pattern: Storage Interface

**Problem:** Hide local disk vs S3 from application logic.

**Real example:** [IBlobStorage](../../../src/NetDropbox.Application/Abstractions/IBlobStorage.cs#L1-L9), wired in [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L33-L53).

**Why it works:** Upload completion saves through an abstraction [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L50-L56).

**Failure modes:** Interface does not expose transactional guarantees; blob writes and DB writes can diverge.

## Pattern: Idempotent Session Start

**Problem:** Retrying upload-start should not create duplicate sessions for the same file/hash.

**Real example:** [StartUploadHandler existing session lookup](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L51-L69).

**Failure modes:** No unique DB index backs the idempotency key, so races can still create duplicates.

## Pattern: Scan-Gated Download

**Problem:** Uploaded bytes should not become downloadable until processing passes.

**Real example:** [FileService.EnsureDownloadable](../../../src/NetDropbox.Application/Files/FileService.cs#L149-L155).

**Failure modes:** Every download path must use the gate; public links also check status [ShareService.OpenPublicDownloadAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L62-L68).

## Pattern: Query Cache With Explicit Invalidation

**Problem:** React must refresh server state after mutations.

**Real example:** [Drive refresh](../../../web/src/App.tsx#L62-L65), [uploadFiles](../../../web/src/App.tsx#L67-L72).

**Failure modes:** Broad invalidation is safe but can refetch more than necessary; missing auth/user in key can matter after account switch.

## Pattern: Problem-Details Exception Mapping

**Problem:** Expected failures should become consistent HTTP error envelopes.

**Real example:** [ExceptionMiddleware](../../../src/NetDropbox.Api/Support/ExceptionMiddleware.cs#L7-L29).

**Failure modes:** Exception-first control flow can hide which errors are part of the public contract.

