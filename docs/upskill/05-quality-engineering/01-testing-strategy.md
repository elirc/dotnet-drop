# Testing Strategy

Current tests cover domain behavior only, for example [FileNodeTests](../../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40). Verified command output shows 6 tests passed in [verification-log](../08-reference/verification-log.md).

| Test type | Belongs here | Example target |
| --- | --- | --- |
| Domain unit | Invariants and state transitions | [FileNode.AddVersion](../../../src/NetDropbox.Domain/FileNode.cs#L51-L62) |
| Application unit/integration | Use-case decisions with EF | [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L78) |
| API integration | Auth, model binding, problem details, streaming | [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) |
| Worker integration | Scan clean/infected/failure states | [ProcessingWorker.ProcessAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L26-L75) |
| Frontend component/e2e | Upload progress, cache invalidation, preview states | [Drive](../../../web/src/App.tsx#L47-L136) |

Do not test EF Core by mocking LINQ heavily; it hides query translation failures. Prefer real provider integration tests for persistence-heavy behavior.

