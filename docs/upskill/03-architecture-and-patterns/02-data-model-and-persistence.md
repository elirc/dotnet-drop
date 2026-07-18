# Data Model And Persistence

## Entities And Relationships

The context exposes users, nodes, versions, blobs, upload sessions/chunks, shares, scan results, and processing jobs [NetDropboxDbContext DbSets](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L9-L18). `FileNode` owns versions through a backing field [NetDropboxDbContext FileNode mapping](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L35-L49), matching the domain collection [FileNode versions](../../../src/NetDropbox.Domain/FileNode.cs#L7-L30).

## Indexes

Confirmed indexes:

- User email unique [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L25-L33).
- File lookup by owner/parent/name/deleted [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L35-L39).
- Blob content hash unique [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L59-L65).
- Upload chunk session/index unique [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L77-L82).
- Share token unique [NetDropboxDbContext](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L84-L91).

## Schema Change Guidance

The dev seeder uses `EnsureCreatedAsync` [DbSeeder](../../../src/NetDropbox.Infrastructure/Persistence/DbSeeder.cs#L8-L24). That makes first-run easy but is not migration discipline. A production migration path should add an initial EF migration, switch startup to `MigrateAsync`, and add a rollback plan.

## Persistence Risks

Search uses `ToLower().Contains` on name and extracted text [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134). That is understandable in a lab; for real scale, use PostgreSQL full-text search or trigram indexes.

