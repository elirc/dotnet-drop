# ANSWER — M2 search at 2 M files

**Q1 predicted plan.** Npgsql translates `x.Name.ToLower().Contains(q)` to `lower(f.name) LIKE '%' || @q || '%'` (with escaping) or `strpos(lower(name), @q) > 0`, depending on the version. Neither can use a B-tree. Postgres may use the `(owner_id, parent_id, name, is_deleted)` index for `owner_id =` only, then filters 2.1 M rows, evaluating `lower()` on each name **and on up to 1 MB of `extracted_text`** (TOASTed, so it must be decompressed). Then comes `Sort` on name plus `OFFSET`. Expect `Buffers: shared read` in the GBs. That's the 4–9 s.

**Q2.**
- `pg_trgm` GIN on `lower(name)` answers substring `%q%` for q of 3+ chars. The index is about 2–3× the name column size; writes are moderately slower. It's the right tool for *names*.
- Trigram on 1 MB of text is huge and slow to update. For content, use `tsvector` (word-based, supports stemming and `ts_rank`): `search_tsv GENERATED ALWAYS AS (to_tsvector('simple', left(coalesce(extracted_text,''), 200000))) STORED` + GIN. It answers *words*, not substrings. Product must accept that "invo" won't match "invoice" in content (a prefix query `invo:*` will).

**Q3.** Two queries (name hits, content hits), merged, with name hits ranked first. Or one `UNION ALL` with a rank column. A single `OR` across two different index types usually becomes a BitmapOr at best, and ordering by name across both loses relevance.

**Q4.** `OFFSET n` scans and discards n rows, so each page costs O(offset). Keyset: `ORDER BY lower(name), id` with `WHERE (lower(name), id) > (@lastName, @lastId)`. The cursor is an opaque base64 of `(lower(name), id)`. Backward compatibility: keep `skip/take` when no `cursor` is given, and add a `nextCursor` via a response header (`X-Next-Cursor`) so the JSON array shape used by `api.search` (`web/src/api.ts:97-99`) doesn't change.

**Q5.** Every keystroke triggers a full scan. Typing "invoice" costs 7 × 5 s queries per user during an incident, which amplifies load. Fix both sides: client debounce ≥ 250 ms plus min length 3 plus TanStack `placeholderData`/`AbortSignal` so stale queries are cancelled (pass `signal` to `fetch`); server min length plus a statement timeout on the search endpoint (`SET LOCAL statement_timeout = '2s'`).

**Seed and verify.** `INSERT … SELECT generate_series(1, 500000)` with random names, and 10 % of rows with a 20 KB text. Record `EXPLAIN (ANALYZE, BUFFERS)` before and after. Look for a `Bitmap Index Scan on ix_…_trgm` and buffer reads down 100×+. Build the indexes `CONCURRENTLY` outside a transaction.

**Query-shape test.** Capture SQL via `ToQueryString()` or an interceptor and assert that it contains `ILIKE`/`LIKE` on `lower(name)` (to match the expression index) and `@@` for content, and does **not** contain `lower(f.extracted_text)`.
