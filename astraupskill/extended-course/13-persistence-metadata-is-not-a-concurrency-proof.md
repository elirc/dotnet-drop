# 13. Persistence metadata is not a concurrency proof

An ORM model can declare indexes, relationships, and concurrency tokens, but each declaration establishes a specific mechanism rather than a complete application guarantee. This chapter reads NetDropboxDbContext as an executable contract candidate and connects it to the actual entity methods. The aim is to design tests that exercise the provider behavior instead of treating configuration names as proof.

Read [NetDropboxDbContext](../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs), [Entity](../../src/NetDropbox.Domain/Common/Entity.cs), and [DependencyInjection](../../src/NetDropbox.Infrastructure/DependencyInjection.cs). The infrastructure registers the context with PostgreSQL through UseNpgsql. Existing handler regression fixtures use EF InMemory. Those providers answer different questions and should not be cited interchangeably.

## Unique indexes protect specific tuples

The model declares uniqueness for user email, file/version number pairs, blob content hash, session/chunk index pairs, share tokens, and direct-share node/recipient pairs. Each constraint can prevent duplicate stored keys at its own boundary. It does not automatically transform a provider exception into a domain conflict or recover an existing winner for the caller.

For example, two first chunk requests can both publish identical bytes and then attempt to add the same session/index metadata. The unique index can reject one insert under the real provider. The handler still needs a policy for recognizing an identical winner and returning a coherent retry outcome. The filesystem's identical-publication acceptance and the database's duplicate-key rejection are different stages.

The file-node owner/parent/name/deleted index is not unique in this model. Its presence can aid a query without enforcing namespace uniqueness. A reviewer who sees HasIndex and assumes uniqueness misses the explicit IsUnique distinction. Tests should be derived from the actual declaration, not from a variable name or expected product behavior.

## A concurrency token must change or be generated

UserStorageAccount and FileNode Version properties are configured as concurrency tokens. The shared Entity base defines a protected uint Version property, but Touch updates only UpdatedAt. The inspected context contains no SaveChanges override that increments Version, and the displayed configuration does not map it as a database-generated row version. These source observations do not establish an effective changing token for ordinary mutations.

Optimistic concurrency normally compares an original token with the stored token during update. If the token never changes, two contexts can both continue to match the same original value. Declaring IsConcurrencyToken is therefore not enough to prove that lost updates are detected. A real-provider test should inspect generated behavior and stored token values before and after competing writes.

Avoid asserting that the application definitely loses every concurrent update without running the relevant path. Other database mechanisms or configurations could matter. The precise finding is an evidence gap in the inspected implementation: the declaration alone does not show how Version advances. A proposed fix needs a defined token owner, update strategy, and response mapping.

## Relationships do not express every reference

The model explicitly configures FileNode.Versions with a foreign key and cascading deletion, using field access for the collection. Many other identifiers appear as scalar properties without a relationship shown in this context. Do not infer a database foreign key for every Guid merely because its name ends in Id. Confirm the actual model or migration before relying on referential enforcement.

Application methods still need meaningful error classification even when constraints exist. A missing blob row can cause SingleAsync to fail, while a business-level not-found result may be more appropriate at a boundary. Constraint enforcement and user-facing error semantics are complementary responsibilities. One prevents an invalid stored shape; the other explains the operation outcome.

Conversely, absence of a configured navigation does not mean no useful invariant can be enforced. A scalar key can participate in a unique index or explicit query. Read the declaration and the use case together. The course's goal is precise evidence, not a stylistic rule that every relationship must have a navigation property.

## Provider differences change what tests establish

The existing upload handler tests use fresh EF InMemory contexts sharing a test database. They can demonstrate that a handler adds records, preserves state on early rejection, and avoids repeating a sequential completed-session charge. They do not exercise PostgreSQL uniqueness, SQL transactions, collation, or multi-connection locking in the same way as the configured production provider.

A domain test is narrower still: it checks local methods without persistence. A provider integration test can establish constraints and concurrency behavior but may still use fake blob and queue ports. A hosted API test adds authentication, binding, validation pipeline, and middleware. Each level should have a claim that justifies its cost.

Do not rerun a broad suite merely because a new conceptual question arose. First identify the smallest fixture capable of falsifying the claim. For Version, two independent contexts loading the same row and attempting updates are more relevant than hundreds of unrelated domain assertions. For duplicate chunk metadata, use the unique key and inspect the losing operation's classification.

## Work a quota contention schedule

Create an account with ten bytes free. Context A and context B each load it and each attempts to reserve seven. Locally, both domain checks can pass against the same initial UsedBytes. The desired persistence contract might allow one commit and reject the other, or use an atomic conditional increment that succeeds only when capacity remains.

If both save without a changing concurrency mechanism, the final value might reflect a lost update rather than the sum, depending on the update strategy. If a proper token changes, one save can receive a concurrency exception. If a conditional database command owns capacity, its affected-row count can decide acceptance. These are alternative mechanisms to test, not interchangeable labels.

The test must inspect both request outcomes and final persisted usage. A final value within quota can still conceal a lost reservation while two files were accepted. Correct accounting relates the usage row to accepted versions, not only to an arithmetic bound on one column.

## Exercise ND-13A: classify model declarations

List each unique index and the invariant it can enforce. Contrast the nonunique file-node index. For one unique constraint, describe the application-level retry or error policy still required after a conflicting write is rejected.

## Exercise ND-13B: prove token behavior

Design a two-context PostgreSQL fixture that records Version before and after a mutation and then attempts a stale write. State the expected result under a proposed changing-token design. Explain why the current IsConcurrencyToken declaration and an EF InMemory test do not by themselves establish that result.

## Exercise ND-13C: preserve the accounting relation

Specify a concurrent quota test that checks accepted file versions, account usage, and both caller outcomes. Compare optimistic-token and atomic-conditional-update designs. Identify which external effects remain outside the database transaction even if the quota row is protected correctly.

## Review checkpoint

A strong answer reads ORM metadata literally and then verifies its execution. It knows that an index may be nonunique, a token needs an update mechanism, and a provider-specific guarantee requires provider-specific evidence. It connects persistence outcomes to application invariants rather than treating a successful SaveChanges as a universal correctness certificate.
