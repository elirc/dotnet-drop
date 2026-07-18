# Refactor And Design Katas

1. **Boundary leak kata:** move [UploadsController.Status](../../../src/NetDropbox.Api/Controllers/UploadsController.cs#L23-L29) into Application without changing behavior.
2. **Outbox kata:** design a table and worker loop replacing [ChannelProcessingQueue](../../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs#L6-L14).
3. **Type-safety kata:** generate frontend types instead of hand-written enum unions [api.ts types](../../../web/src/api.ts#L1-L48).
4. **N+1 kata:** inspect [FileService.ListAsync projection](../../../src/NetDropbox.Application/Files/FileService.cs#L17-L31) and explain generated SQL risks.
5. **RFC kata:** write a one-page RFC for replacing query-string share passwords.

Self-grade: Basic names files. Solid lists tests. Strong includes rollback and operational signals.

