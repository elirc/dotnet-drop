# Risk Register

| Risk | Evidence | Impact | Likelihood | Suggested test | Suggested fix | Confidence |
| --- | --- | --- | --- | --- | --- | --- |
| Lost processing message after upload completion | DB save then queue enqueue [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L82-L84), in-memory queue [ChannelProcessingQueue](../../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs#L6-L14) | Files stuck pending | Medium | Fault-inject crash after DB save | Durable outbox | High |
| Quota oversubscription race | Upload start checks quota [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L35-L40), reserve later [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L79-L80) | User exceeds quota | Medium | Concurrent upload completion test | Quota reservations or DB constraint | Medium |
| Public share password in URL | Password query parameter [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17) | Secret leakage | Medium | Proxy/log review test | POST password exchange | High |
| Search full scans | Lower/contains search [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134) | Slow search | High with data growth | Query plan test | Full-text/trigram index | High |
| Preview object URL leak | Object URL created in effect [PreviewPanel effect](../../../web/src/App.tsx#L221-L238) | Browser memory growth | Medium | Repeated preview memory test | Revoke URL cleanup | High |
| Missing integration coverage | Only domain test evidence [FileNodeTests](../../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40) | Auth/persistence regressions | High | Add API tests | WebApplicationFactory/Testcontainers | High |
| Dev schema creation not migration discipline | Seeder uses `EnsureCreatedAsync` [DbSeeder.SeedAsync](../../../src/NetDropbox.Infrastructure/Persistence/DbSeeder.cs#L10-L15) | Production migration risk | Medium | Migration rehearsal | Add initial migration and use `MigrateAsync` | High |
| No unique idempotency index | Existing session lookup [StartUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L51-L69), upload session indexes [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L67-L75) | Duplicate sessions under race | Medium | Parallel session-start test | Unique index and conflict handling | Medium |

