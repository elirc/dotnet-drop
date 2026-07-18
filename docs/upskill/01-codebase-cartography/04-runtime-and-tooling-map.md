# Runtime And Tooling Map

| Tooling boundary | Evidence | Teaching point |
| --- | --- | --- |
| .NET 8, nullable refs, implicit usings | [Directory.Build.props](../../../Directory.Build.props#L1-L9) | Nullable annotations are part of the contract; juniors should not silence them casually. |
| API packages | [NetDropbox.Api.csproj](../../../src/NetDropbox.Api/NetDropbox.Api.csproj#L1-L18) | Auth, Swagger, Serilog, health checks are API concerns. |
| Application packages | [NetDropbox.Application.csproj](../../../src/NetDropbox.Application/NetDropbox.Application.csproj#L1-L11) | MediatR and FluentValidation live above infrastructure. |
| React/Vite/TanStack Query | [package.json](../../../web/package.json#L6-L24) | Server state belongs in query cache, local UI state in React. |
| Docker local infra | [docker-compose](../../../docker-compose.yml#L1-L79) | Postgres, Redis, RabbitMQ, MinIO exist, but code currently uses in-memory channel queue by default. |
| CI | [ci workflow](../../../.github/workflows/ci.yml#L8-L32) | CI builds backend and frontend, but no Docker/integration test job is present. |

Runtime boundaries:

- Browser hashes files with Web Crypto [sha256](../../../web/src/api.ts#L160-L164).
- API authenticates JWTs [Program](../../../src/NetDropbox.Api/Program.cs#L41-L58).
- Application coordinates persistence and side effects [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86).
- Infrastructure owns EF and storage [DependencyInjection.AddInfrastructure](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L17-L58).
- Worker consumes processing messages [ProcessingWorker.ExecuteAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L18-L24).

