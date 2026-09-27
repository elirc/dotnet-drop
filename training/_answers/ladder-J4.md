# ANSWER — J4 web client: expired sessions and failed uploads

*(Added in B08. Reasoned from `main` source; the reference code below was **not** run: docs-only pass, no `npm install`.)*

**Q1 — 401 trace.** `fetch` resolves with `status 401` → `response.ok` false → `response.json()` usually fails, because the JwtBearer challenge has an empty body → `.catch(() => ({ title: response.statusText }))` → `throw new ApiError("Unauthorized", 401)` (`api.ts:74-76`). Nothing calls `setToken(null)`, so the token stays in `localStorage`. `App` reads the token once into state (`App.tsx:13`) and shows `<Login>` only when it's falsy (`:15`). A reload reads the same stale token again. The user is locked into a broken app until they click Logout or clear storage. `downloadBlob` (`api.ts:149-157`) has its own `fetch` and would need the same handling. Two code paths for one concern is the review smell here.

**Q2 — batch failure.** `Promise.all` rejects on the first rejection. The `await` in `uploadFiles` throws, so `setUploading({})` and `refresh()` (`App.tsx:70-71`) never run. The frozen bars are the stale state. The other uploads keep running in the background (promises aren't cancelled), and some of them succeed, but the list isn't refreshed, so the user can't tell. The rejection is unhandled because the caller is an event handler, so there's no UI message. `Promise.allSettled` waits for every file and returns `{status, value|reason}` per file. Wrap it in `try/finally` so the progress state always clears.

**Q3 — whole-file hash.** `arrayBuffer()` loads the entire file into memory, and `crypto.subtle.digest` has no streaming API. For a 3 GB file that's at least a 3 GB allocation (likely a tab crash on many machines), plus the hashing time before any network activity. Each chunk is then read and hashed again (`App.tsx:280`), which is fine at 4 MB. **Split it out**: fixing it needs an incremental SHA-256 (a WASM or JS library) or a protocol change (per-chunk hashes plus a server-computed final hash), and that touches the `FinalContentHash` contract (`StartUpload.cs:26`). Record it as a follow-up ticket with a measurement (the heap snapshot from curriculum 12).

**Q4 — key collision.** Drop `IMG_0001.jpg` from `Camera/` and another `IMG_0001.jpg` from `Downloads/` in one drag, or drop the same file twice quickly. Both write `uploading["IMG_0001.jpg"]`, so the bars overwrite each other. Key by a per-drop id (index or `crypto.randomUUID()`) and display the name.

**Q5 — test seam.**
- Stubbing `globalThis.fetch` tests the real `request()` logic: status handling, token clearing, error text. It's the right seam for the 401 test.
- Mocking the `api` object is faster for UI tests, but it can't prove the 401 handling, because it bypasses `request()`.
- Mocking React Query proves almost nothing. Wrap components in a real `QueryClientProvider` with `retry: false` so errors surface immediately.

**Reference shape (not run)**
```ts
// api.ts
let onUnauthorized: () => void = () => {};
export function setUnauthorizedHandler(fn: () => void) { onUnauthorized = fn; }
function handle401(response: Response) {
  if (response.status === 401) { setToken(null); onUnauthorized(); }
}
// in request() and downloadBlob(): call handle401(response) before throwing
```
```ts
// App.tsx (inside App)
useEffect(() => setUnauthorizedHandler(() => setTokenState(null)), []);
// uploadFiles
const results = await Promise.allSettled(items.map((f, i) => uploadOne(f, parentId, pct => setUploading(o => ({ ...o, [`${i}:${f.name}`]: pct })))));
try { setFailures(results.flatMap((r, i) => r.status === 'rejected' ? [`${items[i].name}: ${(r.reason as Error).message}`] : [])); }
finally { setUploading({}); refresh(); }
```

**Tests that prove it.**
1. The `fetch` stub returns 401 on `api.files` → assert `localStorage.getItem('netdropbox.token')` is null and the handler was called. Render `<App>` with a token, trigger a query, and `findByRole('button', { name: /sign in/i })` or the login heading.
2. `uploadFiles` with 3 files, where the second `startUpload` returns 500 → assert that two succeeded, one failure message names file 2, and no progress bars remain.
3. `formatBytes(0) === '0 B'`, `formatBytes(1023) === '1023 B'`, `formatBytes(1024) === '1.0 KB'` (`api.ts:166-171`).
Revert the 401 change and confirm test 1 goes red.

**Grading notes.**
- Full marks: the answer handles both `request()` and `downloadBlob()`, uses `allSettled` or an equivalent with `finally`, **splits out** the whole-file hash with a reason, and names the `download()` early-revoke (`App.tsx:300`) as a note.
- Common miss: clearing the token but not re-rendering, because `App` holds the token in React state, so `localStorage` alone doesn't trigger a render.
- The early `revokeObjectURL` after `click()` can cancel the download in some browsers. That's browser-dependent and UNVERIFIED here. Deferring the revoke with `setTimeout` is the usual fix.
