# Good-First Tickets

## Ticket 1: Add Restore Version Domain Test

**Difficulty:** Easy **Time:** 45 minutes **Skills:** xUnit, domain invariants.

**Story:** As a maintainer, I want `RestoreVersion` covered so version rollback behavior stays explicit.

**Read first:** [FileNode.RestoreVersion](../../../src/NetDropbox.Domain/FileNode.cs#L64-L70), [FileNodeTests](../../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40).

**Acceptance criteria:**

- [ ] Test creates two versions.
- [ ] Restores the first version.
- [ ] Asserts current version and pending scan.

## Ticket 2: Add Chunk Validator Tests

Read first: [UploadChunkValidator](../../../src/NetDropbox.Application/Uploads/UploadChunk.cs#L17-L24). Add tests for negative index, empty session id, and wrong checksum length.

## Ticket 3: Improve Preview URL Cleanup

Read first: [PreviewPanel effect](../../../web/src/App.tsx#L221-L238). Add cleanup that revokes object URLs when preview changes. Acceptance: no leaked object URL in repeated preview switching.

## Ticket 4: Move Upload Status Query Out Of Controller

Read first: [UploadsController.Status](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L23-L29). Create a query/service so API remains thin.

## Ticket 5: Add Public Share Password Warning To Docs

Read first: [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17). Document that query-string passwords are dev-only until improved.

## Ticket 6: Add Search Pagination Test

Read first: [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134). Add a test that `take` clamps to 100.

## Ticket 7: Add Dead-Letter Counter Metric Placeholder

Read first: [ProcessingWorker catch](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L67-L75). Add a log field or metric abstraction proposal; do not introduce a new observability package.

## Ticket 8: Add Seed User Note To API README

Read first: [DbSeeder.SeedAsync](../../../src/NetDropbox.Infrastructure/Persistence/DbSeeder.cs#L18-L26), [README seed section](../../../README.md#L18-L23).

