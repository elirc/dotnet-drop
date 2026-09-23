# M2 — Search at 2 million files

**Context.** `FileService.SearchAsync` (`src/NetDropbox.Application/Files/FileService.cs:125-135`) filters `x.OwnerId == userId && !x.IsDeleted && (x.Name.ToLower().Contains(q) || x.ExtractedText.ToLower().Contains(q))`, then orders by name and uses `Skip/Take`. `ExtractedText` holds up to 1 MB per text file (`ProcessingWorker.cs:55`, `ReadTextSampleAsync`). The web client searches on **every keystroke** (`web/src/App.tsx:56-59`, `queryKey` includes `query`).

**Ticket.** "Enterprise tenant Acme has 2.1 M files, 380 k of them with extracted text. Search takes 4–9 s and sometimes times out. Target: p95 < 300 ms for a 3+ character query, result pages of 50, stable ordering when paging."

**Constraints.** Postgres only (no Elasticsearch this quarter). Index builds must not lock `file_nodes` for writes. Existing clients call `?q=&skip=&take=`.

**Definition of done.**
- `EXPLAIN (ANALYZE, BUFFERS)` before and after on a seeded dataset (script included) with at least 500 k rows for one user.
- Index DDL created `CONCURRENTLY`, with the migration path described.
- Keyset pagination added in a backward-compatible way.
- Client debounce (≥ 250 ms) and a min-length rule.
- A test that the generated SQL uses the intended operator. A query-shape test, not a timing test.

**Explain before touching.**
1. Predict the plan for today's query at 2 M rows. Which index is used, if any? What do `lower()` and `LIKE '%q%'` do to index usability?
2. `pg_trgm` GIN on `lower(name)` versus `tsvector` full-text on `extracted_text`. What queries does each answer, and what does each cost on write and storage?
3. Should content search and name search be one query or two? What happens to relevance ordering?
4. Why does `OFFSET 5000` get slower as it grows, and what exactly is the keyset cursor here?
5. What does searching on every keystroke do to the DB during an incident, and whose job is it to fix: client, server, or both?

<details><summary>Hints</summary>

- `CREATE EXTENSION pg_trgm; CREATE INDEX CONCURRENTLY … USING gin (lower(name) gin_trgm_ops) WHERE NOT is_deleted;` EF: `EF.Functions.ILike`, or `lower(name) LIKE`.
- A generated column `search_tsv tsvector GENERATED ALWAYS AS (to_tsvector('simple', coalesce(extracted_text,''))) STORED` with a GIN index. Mind the 1 MB text: consider truncating or storing only the tsvector.
</details>

Answer: `training/_answers/ladder-M2.md`
