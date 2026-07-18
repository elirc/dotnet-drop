# Architecture Critique

Provenance verdict: synthetic. Be stricter than you would be with a messy real legacy repo: clean structure can hide missing integration depth.

## Strong Choices

| Choice | Evidence | Why it is strong |
| --- | --- | --- |
| Layered backend | [API project refs](../../../src/NetDropbox.Api/NetDropbox.Api.csproj#L1-L18) | Separates HTTP from use cases and infrastructure. |
| Domain methods for state | [FileNode](../../../src/NetDropbox.Domain/FileNode.cs#L51-L104) | State transitions are named and greppable. |
| Streaming downloads | [FilesController.Download](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L55-L59) | Avoids buffering full files in memory. |
| Storage abstraction | [DependencyInjection storage switch](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L33-L53) | Lets local and S3-compatible storage share application code. |

## Prioritized Improvements

| Priority | Recommendation | Evidence | Migration/test strategy |
| --- | --- | --- | --- |
| 1 | Add integration tests for upload/download/share auth paths. | Only domain tests were found [FileNodeTests](../../../tests/NetDropbox.Tests/Domain/FileNodeTests.cs#L8-L40). | Add WebApplicationFactory or Testcontainers tests around pending/clean/infected download. |
| 2 | Replace in-memory queue with durable outbox or RabbitMQ consumer. | Singleton channel queue [DependencyInjection](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L24-L28). | First persist outbox table; worker drains outbox; keep channel as local adapter during migration. |
| 3 | Back upload idempotency with a unique index. | Lookup but no unique index on idempotency fields [StartUploadHandler](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L51-L69), [UploadSession mapping](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L67-L75). | Add migration, catch unique violation, return existing session. |
| 4 | Move search to PostgreSQL full-text or trigram. | `ToLower().Contains` over extracted text [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134). | Add indexed generated vector or trigram index; compare query plans before/after. |
| 5 | Harden public share password flow. | Password accepted in query string [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17). | Use POST to exchange password for short-lived access token; keep old route temporarily. |

## Confirmed vs Hypothesis

- Confirmed: no HEAD commit; synthetic provenance; build/test/frontend build pass.
- Confirmed: no integration tests opened; only domain tests are present in test project files inspected.
- Hypothesis: upload completion can leave orphaned DB/blob/queue states under crash because there is no outbox or transaction spanning storage. Needs fault-injection test.

## If I Owned This For Three Months

1. Stabilize integration tests around the five key flows.
2. Introduce durable processing outbox.
3. Add migrations and production schema discipline.
4. Add authorization regression suite for every endpoint accepting ids.
5. Replace naive search and add query-plan checks for browse/search.

