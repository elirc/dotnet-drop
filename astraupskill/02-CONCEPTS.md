# Concepts: integrity, immutable chunks, and quota preflight

A digest and a length answer different questions. Matching SHA-256 against the declared digest verifies that the assembled content matches the declared content identifier. It does not establish that the caller declared a truthful size for quota accounting. Completion therefore compares observed bytes with TotalSize independently. A four-byte declaration with an eight-byte body must fail even when the declared digest belongs to those eight bytes. The regression deliberately constructs that situation and verifies that external blob storage was never called.

Idempotency means a retry preserves the previously accepted result. It does not mean overwriting a file and hoping the database rejects duplicate metadata later. Two requests can both observe that an UploadChunk row is absent. If each then replaces the same path, the database winner and filesystem winner can differ. Netdrop instead publishes with a non-overwriting move. When a destination already exists, it inspects that file. Equal size and digest make the retry acceptable; different content produces ChunkWriteConflictException. The accepted bytes remain immutable even before database metadata is recovered from an interrupted attempt.

Overflow is another integrity boundary. Adding requested bytes to UsedBytes before comparing with QuotaBytes can wrap a signed integer. EnsureCanReserve checks existing usage and compares the requested amount with remaining capacity using subtraction. It changes no state. Reserve repeats the same check and updates usage only in the later persistence sequence. Chunk rounding similarly uses the positive total minus one before dividing, then checks whether the resulting count fits an integer.

Atomicity must always name its resource. A filesystem publication protects the chunk path. An EF SaveChanges groups tracked database changes according to its provider. Neither makes the external blob write and later queue call part of one global transaction. The implementation intentionally avoids deleting a content-addressed blob after an uncertain database failure because another upload may reference that same key. A durable compensation or outbox design requires its own ownership and recovery protocol. These limits are design facts, not reasons to weaken the local validation guarantees.

## Source excerpt

From [src/NetDropbox.Domain/UserStorageAccount.cs](../src/NetDropbox.Domain/UserStorageAccount.cs).

```cs
public void Reserve(long bytes)
    {
        EnsureCanReserve(bytes);
        UsedBytes += bytes;
        Touch();
    }

    public void EnsureCanReserve(long bytes)
    {
        if (bytes < 0) throw new DomainException("Bytes cannot be negative.");
        if (UsedBytes > QuotaBytes || bytes > QuotaBytes - UsedBytes)
            throw new DomainException("Storage quota exceeded.");
    }

    public void Release(long bytes)
    {
        if (bytes < 0) throw new DomainException("Bytes cannot be negative.");
        UsedBytes = Math.Max(0, UsedBytes - bytes);
        Touch();
    }
}
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)
