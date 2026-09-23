# J1 — Upload status: 404 not 500, and out of the controller

**Context.** `UploadsController.Status` (`src/NetDropbox.Api/Controllers/UploadsController.cs:23-29`) queries `IAppDbContext` directly. It's the only controller that does; every other endpoint goes through MediatR or a service. It uses `SingleAsync`.

**Ticket (PM).** "When a resume link points at an upload that isn't the user's, or doesn't exist, the app shows 'An unexpected error occurred'. It should say the upload wasn't found. While you're there, make this endpoint look like the others."

**Constraints.** The response shape (`UploadSessionDto`, `src/NetDropbox.Application/Dto/FileDtos.cs:25-31`) must not change, because `web/src/api.ts` depends on it. No new packages in Application.

**Definition of done.**
- `GetUploadStatusQuery` plus a handler in `Application/Uploads`, with the same owner check as `UploadChunkHandler`.
- A missing or foreign session returns `404` problem+json. Decide whether an expired or completed session returns 200, 404 or 410, and justify the choice.
- A handler unit test (404 path, owner path), plus one HTTP-level test (`WebApplicationFactory`) that asserts status code and `application/problem+json`.

**Explain before touching.**
1. Trace how `SingleAsync` becomes a 500 today. Name each file the exception passes through.
2. Returning 404 rather than 403 for *someone else's* session: what does that prevent?
3. What crosses the API → Application boundary after your change, and what stops crossing it?
4. What does the HTTP test need that `tests/NetDropbox.Tests/NetDropbox.Tests.csproj` lacks today?
5. How does `api.ts` `request()` turn a problem response into a user message? Will your change improve the UI text with no client change?

<details><summary>Hints</summary>

- `src/NetDropbox.Api/Support/ExceptionMiddleware.cs:17-23`.
- The house pattern: `SingleOrDefaultAsync(...) ?? throw new AppException("...", HttpStatusCode.NotFound)`.
- `WebApplicationFactory<Program>` needs `public partial class Program {}` and a database (J0 first).
</details>

Answer: `training/_answers/ladder-J1.md`
