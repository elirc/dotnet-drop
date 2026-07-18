# Systematic Debugging

Use: reproduce -> narrow -> hypothesize -> test cheaply -> fix root cause -> add regression coverage.

## Scenario: Upload Completes But File Stays Pending

**Reproduction:** upload a file and see pending scan indefinitely.

**First question:** did the job get persisted or did the queue lose the message?

**Narrowing path:** inspect [CompleteUploadHandler enqueue](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L74-L84), then [ProcessingWorker.ExecuteAsync](../../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs#L18-L24), then job status in DB.

**Likely root causes:** app restarted after DB save; worker exception; queue singleton not populated.

**Regression test:** crash/failure test around the gap after `SaveChangesAsync`.

## Scenario: User Can See Another User's File

First inspect every route accepting an id. Owner filters should resemble [FileService.GetOwnedNode](../../../src/NetDropbox.Application/Files/FileService.cs#L143-L146) or [ShareService.CreateLinkAsync](../../../src/NetDropbox.Application/Shares/ShareService.cs#L11-L20). If a controller queries context directly, scrutinize it.

## Scenario: Large Browser Upload Freezes

Start with [sha256](../../../web/src/api.ts#L160-L164). Hypothesis: `arrayBuffer()` loads the full file into memory. Test with browser performance panel and a large file.

## Scenario: Search Is Slow

Start with [FileService.SearchAsync](../../../src/NetDropbox.Application/Files/FileService.cs#L125-L134). Enable EF query logging, inspect SQL, then add an index or full-text strategy.

## Scenario: Public Link Password Appears In Logs

Start with [PublicSharesController.Download](../../../src/NetDropbox.Api/Controllers/PublicSharesController.cs#L11-L17). Query strings can be logged by proxies; propose POST password exchange.

