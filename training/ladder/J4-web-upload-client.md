# J4 — Web client: expired sessions and failed uploads fail visibly

*(Added in B08 to fill the "web/TS thin" gap in batch1report. It's the only ladder ticket whose code lives entirely in `web/`.)*

**Context.** The SPA is two files: `web/src/App.tsx` (UI plus `uploadOne`) and `web/src/api.ts` (`request()`, `downloadBlob()`, `sha256()`). `web/package.json` has no test runner at all. Two support complaints landed the same week:

> "I left the tab open overnight. This morning every folder shows an error and refreshing doesn't help. I had to clear my browser data."

> "I dropped 12 photos. One failed, and the progress bars froze forever. I couldn't tell which files made it."

**Ticket (PM).** "When the session expires, send people back to the login screen. When an upload fails, tell them which file failed and let the rest finish. Add whatever testing setup the web app needs so this stays fixed."

**Constraints.** Don't change any API route or DTO. Keep React 18 and React Query. New dev dependencies are fine (Vitest, Testing Library, jsdom). No new runtime dependencies.

**Definition of done.**
- A 401 from any API call (including `downloadBlob`) clears the stored token and shows `<Login>` without a manual reload.
- A batch upload reports success or failure **per file**. One failure doesn't hide the others' results, and the progress UI always clears. Two files with the same name from different drops don't overwrite each other's progress.
- `npm test` runs at least 3 tests: the 401 path, a mixed success/failure batch, and one for `formatBytes` edge cases (0, 1023, 1024).
- `npm run build` (which runs `tsc -b`) still passes.

**Explain before touching.**
1. Trace a 401 through `api.ts:67-82`. What gets thrown, what stays in `localStorage`, and why does `App.tsx:13-17` never show the login form again?
2. In `uploadFiles` (`App.tsx:67-72`), what happens to `setUploading({})` and `refresh()` when one `uploadOne` rejects? What's the difference between `Promise.all` and `Promise.allSettled` here?
3. `sha256(file)` (`api.ts:160-164`) calls `blob.arrayBuffer()` on the **whole file** before the first chunk is sent. What does that mean for a 3 GB video, both in memory and in time before the progress bar moves? Should that be fixed in this ticket or split out? Justify.
4. Progress is keyed by `file.name` (`App.tsx:69`). Give a concrete drop that makes two bars share one key.
5. How will your test fake the API? Name the seam (`fetch`, the `api` object, or React Query) and say what each choice can and can't prove.

<details><summary>Hints</summary>

- A module-level `onUnauthorized` callback in `api.ts` that `App` registers is enough. You don't need a context provider.
- `download()` (`App.tsx:293-301`) revokes the object URL immediately after `click()`. Note it in your PR description even if you don't fix it.
- `vitest` + `@testing-library/react` + `jsdom`; stub `globalThis.fetch` with `vi.fn()`.
- See [curriculum 12, frontend performance](../../../opusorganize/apprenticeship/curriculum/12-frontend-performance.md) for how to measure question 3 instead of guessing.
</details>

Answer: `training/_answers/ladder-J4.md`
