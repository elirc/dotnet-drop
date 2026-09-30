# 16. Search results are a changing read model

Read the Search and List methods in [FileService](../../src/NetDropbox.Application/Files/FileService.cs). These methods are easy to treat as presentation details because they do not directly write blobs. In fact, they expose the consequences of version changes, scan timing, name normalization, and deletion policy. This chapter develops a precise read-model contract and a way to test it without assuming a full search engine.

The prerequisite is chapter 08's distinction between current version and node-level scan metadata. Search combines the node name and extracted text; it does not independently determine which version produced that text. That relationship must be considered when deciding what a result means. A result can be an accurate projection of stored fields while still misleading a user about the current bytes.

## Follow one query through the method

The search input is trimmed and converted to lower case. The query restricts results to the owner and excludes deleted nodes. It matches either a lower-cased name containing the search text or non-null extracted text containing it. It orders by name, applies Skip, and clamps Take into the supported range. The returned DTO selects information about the current version where available.

Each operation has a distinct consequence. Owner filtering is an authorization boundary within this query. The deletion predicate is a visibility rule. Text normalization is a matching decision. Ordering and pagination determine traversal behavior. DTO selection determines which file metadata accompanies a hit. Testing only that one matching file appears covers a small part of this contract.

An empty query is especially instructive. An empty string is contained in every string in ordinary string semantics. The API supplies an empty query when none is provided. The translated database behavior still deserves provider-backed verification, but the source clearly does not introduce a separate "empty search returns no results" branch. A learner should predict broad matching rather than inventing such a branch.

Case conversion also does not establish a universal linguistic search policy. The application's expression, the database provider's translation, and database collation all contribute. Use ordinary ASCII examples for a first unit fixture. For international behavior, create explicit examples and run them against the intended provider. Do not label one in-memory test as proof of production collation.

## Work a changing-page trace

Start with four visible nodes named Atlas, Birch, Cedar, and Delta. A caller asks for two results at offset zero and receives Atlas and Birch. Before the next request, another caller inserts Aardvark. The second request at offset two can now include Birch again. Offset pagination describes positions in the current ordered set; it does not bind subsequent calls to the earlier snapshot.

Deletion can cause the complementary omission. If Atlas disappears after page one, offset two begins later in the smaller set and can skip Cedar. Neither trace requires a broken database. They arise from changes between requests. A good UI can tolerate and explain some movement, while an export or reconciliation process may require a stronger traversal contract.

Ordering only by Name introduces a separate issue: ties. Two nodes can have equal names because the inspected index is not a unique sibling-name constraint and some operations do not check duplicates. An explicit secondary key, such as an immutable identifier, would make tie order defined. It would not by itself create a snapshot across requests. Keep deterministic ordering and snapshot consistency as different design decisions.

A proposed cursor could contain the last name and identifier. The next query would select strictly after that pair under the same ordering. This avoids some offset shifts but does not freeze renames or deletions. A node renamed across the cursor boundary can still move. If the requirement is "each item as of the export start exactly once," a stable snapshot or materialized export set is a stronger proposal than a cursor alone.

## Searchable does not mean downloadable

The search predicate does not include the same scan-status gate used by owner download. A pending or failed node may still match its name. Because extracted text can remain on a node through some transitions, it can also influence matching after a version change. These are observations about the current query and domain mutations, not an assertion that every such result leaks data to another owner.

Ownership filtering still matters: the query is scoped to its owner. The product question is what information that owner should see while a new version is pending or has failed. Showing the name with a pending badge may be appropriate. Showing an old extracted-text match as though it describes the current version may be confusing. A proposed search DTO could expose the provenance or status of searchable text.

Construct a worked fixture with a file named Notes, a first version containing a synthetic phrase, and a second version with unrelated bytes. Record the node's extracted text before and after AddVersion. Ask whether a search for the old phrase can still match before processing completes. Then distinguish the query result from the download gate. One can succeed while the other refuses access without any inconsistency in the inspected branches.

The repair is not automatically "clear everything." Keeping old metadata may support a useful version-history experience. The contract must say whether search targets current content, all versions, or the last successfully processed version. Once that policy is explicit, fields and DTOs can make the result honest. An implementation that silently changes between those meanings is difficult to test and explain.

## Build a read-model test portfolio

A small direct query fixture should cover owner separation, deleted visibility, name match, extracted-text match, no match, and current-version projection. A provider-backed fixture should cover case behavior, ordering ties, and translation. A multi-request scenario should cover insertion or deletion between pages. These layers answer different questions and should not be collapsed into one large test with vague assertions.

For performance, count database queries and inspect the translated query before proposing an index. A name Contains expression can have different index implications from equality or prefix matching. An index recommendation without a query plan and representative data is a hypothesis. The course does not require loading a large dataset into the user's database; a future disposable benchmark can use generated synthetic names and text.

Avoid asserting exact wall-clock thresholds in the first correctness suite. Such thresholds combine machine speed, caching, provider setup, and query work. Prefer structural evidence first: bounded result size, no unexpected per-row calls, stable query shape, and correct filtering. Performance targets can follow when the workload and environment are specified.

## Exercise ND-16A: predict two page traversals

Use the four-node example to show one duplicate caused by insertion and one omission caused by deletion. Then introduce two equal names. Explain which problem a secondary key fixes and which problems remain even after adding that key.

## Exercise ND-16B: define text provenance

Choose whether search should target current content, every version, or the last clean version. Give a DTO example in words, a transition trace after AddVersion, and a test that distinguishes your choice from the other two policies. Do not claim the current application already implements your chosen model.

## Exercise ND-16C: propose an export traversal

Design a read-only export of an owner's metadata. State whether it is best effort or snapshot-based, what happens to renames during traversal, how duplicate names are ordered, and how a caller resumes after interruption. Name the storage evidence required for a snapshot claim.

## Review checkpoint

Search is part of the domain's explanation to the user. A strong review can connect a matching field to its provenance, a page to its consistency promise, and a performance claim to measured evidence. It does not confuse a cursor with a snapshot or an owner filter with every other visibility rule.
