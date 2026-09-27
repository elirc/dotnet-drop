# NetDropbox on-ramp: 8 junior warm-ups

Do these before `training/`. Each takes 10–25 minutes. You only need to read the code (from `main`: use `git show main:<path>` if your working tree has the uncommitted Astra changes). Exercise 6 is the only one that needs `dotnet`.
Write your answers in your own notes file, then check them against `learn/_answers/00-onramp.md`.

Line numbers refer to committed `main` (`d612e0b` source, unchanged by the training commits).

---

## 1. Predict the output: the quota meter (10 min)
Read `src/NetDropbox.Domain/UserStorageAccount.cs:35-48`. Predict `UsedBytes` after each line, or say which line throws and what the message is:

```csharp
var u = UserStorageAccount.Create("A@Example.com ", "A", 10);
u.Reserve(8);
u.Reserve(3);          // (a)
u.Release(20);         // (b) — assume (a) was caught
u.Reserve(10);         // (c)
Console.WriteLine(u.Email);  // (d)
```
Then run `grep -rn "Release(" src` (or search in your editor). How many production callers does `Release` have? What does that tell you about how the meter can ever go down?

## 2. Trace a request: creating a duplicate folder (15 min)
A logged-in user sends `POST /api/v1/files/folders` with `{ "parentId": null, "name": "Taxes" }` and a folder called `Taxes` already exists in their root.
Name, in order, every file and line the request passes through until the browser shows an error message. Start at `FilesController.cs:23-25` and end at `web/src/api.ts:74-76`.
Say which HTTP status code comes back and what text the user sees.
Bonus: two browser tabs send the same request at the same millisecond. Can two `Taxes` folders get created? Look at `FileService.cs:36-41` and the index at `NetDropboxDbContext.cs:38`. Is that index unique?

## 3. Read a guard and find the gap (15 min)
Read `FileNode.Move` (`src/NetDropbox.Domain/FileNode.cs:44-49`) and `FileService.MoveAsync` (`src/NetDropbox.Application/Files/FileService.cs:52-62`).
- (a) Can the guard on line 46 ever fire when the move comes through `MoveAsync`? Explain using line 58.
- (b) A user has folder `A` containing folder `B`. They move `A` into `B`. Which checks run, and does the move succeed? What does the tree look like afterwards?
- (c) Which ladder ticket is about this? (Look in `training/ladder/README.md`. Don't open its answer.)

## 4. Explain the schema (20 min)
Using only `src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs` and `src/NetDropbox.Domain/Common/Entity.cs`:
- (a) List every **unique** index and say, in plain English, which duplicate each one prevents.
- (b) How many foreign keys does the model declare? Which one? What stops a `file_nodes.parent_id` from pointing at a row that doesn't exist?
- (c) `Version` is marked `IsConcurrencyToken()` on two tables (lines 32 and 43). Search the Domain project for any code that assigns `Version`. With EF's concurrency-token behavior in mind, what `WHERE` clause will EF add to an `UPDATE file_nodes`, and when would that update ever affect 0 rows?
- (d) Why is `ExtractedText` mapped as `text` (line 40), and where does its content come from? (Hint: `ProcessingWorker.cs:55`.)

## 5. List the side effects of one handler (20 min)
Read `CompleteUploadHandler.Handle` (`src/NetDropbox.Application/Uploads/CompleteUpload.cs:28-87`).
- (a) List every side effect in order: file system, blob store, database rows, queue. Give the line number of each.
- (b) Which of them happen inside the single transaction at line 82, and which happen before or after it?
- (c) The process crashes right after line 82 finishes. Which work is lost, and which row will say something that isn't true?
- (d) The blob is saved at line 54, but `SaveChangesAsync` at line 82 then fails. What's left behind in the blob store?
Link: [01 transactional outbox](../../opusorganize/apprenticeship/curriculum/01-transactional-outbox.md).

## 6. Write one characterization test (25 min, needs `dotnet`)
A characterization test pins what the code **does today**, not what it should do. See [04 characterization tests](../../opusorganize/apprenticeship/curriculum/04-characterization-tests.md).
In a worktree (`git worktree add ../netdrop-onramp -b learn/onramp main`), add `tests/NetDropbox.Tests/Domain/BlobObjectTests.cs` in the same style as `UserStorageAccountTests.cs`. Write one test per current behavior:
- `BlobObject.Create(...)` starts with `RefCount == 1` (`BlobObject.cs:14`).
- Calling `RemoveReference()` twice on a new blob leaves `RefCount == 0` and doesn't throw (`BlobObject.cs:35-40`).
- `UploadSession.Create` with `totalSize: 0` gives `TotalChunks == 1` (`UploadSession.cs:40`).
Run `dotnet test tests/NetDropbox.Tests` (set `DOTNET_ROLL_FORWARD=Major` if you only have the .NET 10 runtime). Then answer: is "a zero-byte upload still has one chunk" a behavior you'd keep or a bug? Who would you ask?
Proof it's a real test: change `RefCount = 1` to `RefCount = 0` in a scratch edit, watch it go red, and revert.

## 7. Resume logic: two edge cases (15 min)
Read `StartUploadHandler.Handle` (`src/NetDropbox.Application/Uploads/StartUpload.cs:33-78`), `UploadSession.cs:15` and `:44`, and `JanitorWorker.cs:26,37-44`.
- (a) The resume lookup (lines 51-58) matches on `FinalContentHash == request.FinalContentHash`. The session stores the hash lower-cased (`UploadSession.cs:15`). What happens if a client sends the hash in upper case? Does the real client (`web/src/api.ts:160-164`) ever do that?
- (b) A session's `ExpiresAt` passed 5 minutes ago, but the janitor hasn't run yet. The user hits upload again. Walk through `StartUpload` → chunk PUTs → `CompleteUpload.cs:33`. What does the user see? What's the longest this window can last?

## 8. Frontend: what happens after 8 hours? (15 min)
Tokens expire after 8 hours (`AuthController.cs:30`). Read `web/src/api.ts:58-82` and `web/src/App.tsx:12-20`.
- (a) The user left the tab open overnight and clicks a folder. What status does the API return, what does `request()` do with it, and what's on screen?
- (b) Is the expired token removed from `localStorage`? How does the user get back to the login form?
- (c) Where would you put the fix, and what would you test? (This is ladder J4. Write your answer here first.)

---
When you're done: [`learn/_answers/00-onramp.md`](_answers/00-onramp.md). Then go to Session 4 in `learn/README.md`.
