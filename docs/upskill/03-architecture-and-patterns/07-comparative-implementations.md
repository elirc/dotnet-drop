# Comparative Implementations

## Comparison 1: Direct DbContext In Application vs Repository Layer

**The decision this repo made:** application code talks to `IAppDbContext` directly - [StartUploadHandler](../../../src/NetDropbox.Application/Uploads/StartUpload.cs#L31-L78), [FileService](../../../src/NetDropbox.Application/Files/FileService.cs#L11-L31).

**The alternative:**

```csharp
// Illustrative: not from this repo
public interface IFileNodeRepository
{
    Task<FileNode?> GetOwnedAsync(Guid userId, Guid nodeId, CancellationToken ct);
    Task<IReadOnlyList<FileNodeDto>> ListAsync(Guid userId, Guid? parentId, bool trash, CancellationToken ct);
}
```

| Dimension | This repo's choice | Alternative |
| --- | --- | --- |
| Complexity / ceremony | Low. LINQ stays visible. | More files and naming decisions. |
| Testability | Handler tests need EF test double or real DB. | Repositories can be mocked, but query behavior may be faked incorrectly. |
| Performance | Query shape visible near use case. | Query performance can hide behind methods. |
| Coupling & blast radius | Application knows EF concepts like `Include` and `AsNoTracking`. | Application depends on custom repo contracts. |
| Failure modes | Business code can grow query duplication. | Anemic repositories can become pass-through noise. |
| Migration cost | Moderate: `FileService` and upload handlers use `IAppDbContext` directly. | Add repo interfaces, move queries gradually. |

**Verdict for this repo:** right call for a small synthetic lab. Switch when query reuse, multi-tenant filters, or integration-test setup becomes painful.

**Drill:** Refactor only `FileService.GetOwnedNode` behind an `IFileAuthorizationQueries` interface on a scratch branch. Self-grade: Basic compiles; Solid preserves behavior; Strong adds an authorization regression test.

## Comparison 2: Controllers + MediatR vs Minimal APIs

**The decision this repo made:** controller classes delegate to services/handlers - [UploadsController](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L12-L40), [FilesController](../../../src/NetDropbox.Api/Controllers/FilesController.cs#L9-L69).

**The alternative:**

```csharp
// Illustrative: not from this repo
app.MapPost("/api/v1/uploads", async (StartUploadRequest request, IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
    await mediator.Send(new StartUploadCommand(user.UserId(), request.ParentId, request.FileName, request.TotalSize, request.MimeType, request.FinalContentHash, request.ChunkSize), ct));
```

| Dimension | Controllers | Minimal APIs |
| --- | --- | --- |
| Complexity / ceremony | Familiar attributes and classes. | Less ceremony for small endpoint sets. |
| Testability | Controller unit tests possible but low value here. | Endpoint tests often push toward integration tests. |
| Performance | Fine; overhead is not meaningful here. | Slightly less overhead, rarely decisive. |
| Coupling | Route grouping by controller. | Can centralize endpoint maps per feature. |
| Failure modes | Controller classes can accumulate logic. | Program/endpoint files can become long. |
| Migration cost | Low per endpoint, but Swagger/auth conventions must be preserved. | Move route groups gradually. |

**Verdict for this repo:** controllers are defensible because the app has file/share/upload groupings. Switch only if endpoint modules become more feature-sliced.

## Comparison 3: In-Memory Channel Queue vs Durable Outbox/RabbitMQ

**The decision this repo made:** `IProcessingQueue` is implemented as a singleton channel [ChannelProcessingQueue](../../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs#L1-L12), registered in [DependencyInjection](../../../src/NetDropbox.Infrastructure/DependencyInjection.cs#L24-L28).

**The alternative:**

```csharp
// Illustrative: not from this repo
await db.OutboxMessages.AddAsync(new OutboxMessage("FileUploaded", payload), ct);
await db.SaveChangesAsync(ct);
// Worker polls or RabbitMQ publisher drains committed outbox rows.
```

| Dimension | Channel queue | Durable outbox/RabbitMQ |
| --- | --- | --- |
| Complexity / ceremony | Very low. | More schema, worker state, delivery semantics. |
| Testability | Easy unit tests; weak crash tests. | Integration tests can assert replay. |
| Performance | Fast in-process. | More latency and operational overhead. |
| Coupling | Tied to single process lifetime. | Decouples API commit from worker execution. |
| Failure modes | Lost message on process crash. | Duplicate delivery, poison messages, backpressure. |
| Migration cost | Medium. `ProcessingJob` already exists, which helps. | Add outbox and idempotent worker. |

**What each commits you to:** channel queue commits operators to "best effort" processing. Durable queue commits developers to idempotency, retries, and monitoring.

**Verdict for this repo:** acceptable for a lab, questionable for the product spec's reliability goal.

**Migration drill:** Write a plan to move upload processing to outbox.

Self-grade:

- Basic: add outbox table and enqueue rows.
- Solid: worker marks rows processed, retries with backoff, and handles duplicates.
- Strong: migration includes rollback, dashboard/health signal, and tests for crash after [CompleteUploadHandler SaveChangesAsync](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L82-L84).

## Comparison 4: JWT Local Storage vs Cookie Session

**The decision this repo made:** API validates JWT bearer tokens [Program](../../../src/NetDropbox.Api/Program.cs#L41-L58), frontend stores token in localStorage [api token helpers](../../../web/src/api.ts#L58-L65).

**The alternative:**

```csharp
// Illustrative: not from this repo
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => options.Cookie.HttpOnly = true);
```

| Dimension | JWT + localStorage | HttpOnly cookie |
| --- | --- | --- |
| Complexity | Simple SPA fetch header. | CSRF and same-site settings matter. |
| Testability | Easy to inject token in tests. | Browser-like tests closer to reality. |
| Security | XSS can steal token. | CSRF must be handled; token not readable by JS. |
| Coupling | Client owns auth header. | Server owns session cookie behavior. |
| Migration cost | Medium: login and fetch wrapper change. | Add cookie auth and CSRF strategy. |

**Verdict for this repo:** defensible for a training app; revisit before production.

## Comparison 5: React Query vs Hand-Rolled Fetch State

**The decision this repo made:** TanStack Query manages file, usage, version state [Drive queries](../../../web/src/App.tsx#L56-L60), [PreviewPanel versions](../../../web/src/App.tsx#L216-L219).

**The alternative:**

```tsx
// Illustrative: not from this repo
const [files, setFiles] = useState<FileNode[]>([]);
const [loading, setLoading] = useState(false);
useEffect(() => { setLoading(true); api.files(parentId).then(setFiles).finally(() => setLoading(false)); }, [parentId]);
```

| Dimension | React Query | Hand-rolled |
| --- | --- | --- |
| Complexity | Extra library, query-key discipline. | Simple at first. |
| Testability | Cache behavior is explicit. | More component-specific state to assert. |
| Performance | Dedupes/refetches predictably. | Easy to overfetch or stale-cache. |
| Coupling | Components depend on query client. | Components own network lifecycle. |
| Migration cost | Low in this small client. | Would remove invalidation semantics. |

**Verdict for this repo:** right choice. Switch signal: almost none; instead improve query keys and cleanup.

