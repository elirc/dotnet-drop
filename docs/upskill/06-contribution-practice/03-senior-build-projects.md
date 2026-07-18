# Senior Build Projects

## Project 1: Production-Ready Upload Consistency

Problem: upload completion crosses DB, blob storage, chunk cleanup, and queue [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L45-L84).

Value: reduces orphaned data and stuck pending scans.

Design checklist: transaction boundary, outbox, idempotent blob save, cleanup retries, metrics, rollback plan.

## Project 2: Authorization Regression Suite

Problem: resource security depends on owner filters such as [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146) and [ShareService.CreateLinkAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L11-L20).

Value: prevents IDOR regressions.

Deliverables: matrix of endpoints, user A/B fixtures, public-token cases, CI gate.

## Project 3: Search Platform

Problem: current search is simple string matching [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134).

Value: better performance and relevance.

Deliverables: migration, index strategy, query plan comparison, fallback/rollback, UI empty/error states.

## Project 4: Operational Processing Dashboard

Problem: processing jobs have statuses but no operator surface [NetDropboxDbContext ProcessingJob mapping](../../../src/NetDropbox.Infrastructure/Persistence/NetDropboxDbContext.cs#L108-L115).

Value: makes failures visible.

Deliverables: admin API, filtered list, retry dead-lettered job, audit log, security policy.

