# 11. Processing attempts and queue lifetime

Netdrop records processing jobs in the database and passes messages through an in-process channel. Those two forms of state have different lifetimes. This chapter follows the worker from dequeue through running, scanning, completion, and retry, then identifies the evidence needed for stronger delivery guarantees. It does not turn the simulated scanner into a production antivirus service.

Read [ChannelProcessingQueue](../../src/NetDropbox.Infrastructure/Processing/ChannelProcessingQueue.cs), [ProcessingWorker](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs), and [ProcessingJob](../../src/NetDropbox.Domain/ProcessingJob.cs). The queue uses an unbounded Channel. Enqueue writes a message to that channel; dequeue reads it as an asynchronous sequence. The queue itself contains no durable storage or restart recovery.

## A job row and a message are not interchangeable

The completion handler adds a ProcessingJob record before saving the completed file graph, then enqueues a ProcessingMessage afterward. A crash between those operations can leave a row without a channel message. A process restart also loses messages still only in channel memory. The inspected worker consumes messages; it does not begin by querying every queued database job for recovery.

This is why a durable job row is useful but insufficient by itself. Recovery needs an actor that discovers outstanding obligations and safely schedules them. A proposed dispatcher could query eligible rows, claim work, and retry delivery. That proposal must coordinate multiple workers and avoid reprocessing completed work blindly. The current channel does not supply those guarantees automatically.

An unbounded queue avoids a capacity rejection at enqueue under ordinary conditions, but it can accumulate memory if producers outrun the worker. It does not establish a bounded processing latency or resource budget. A bounded-channel proposal would introduce backpressure and failure behavior that callers must handle. Capacity is a product and operational policy, not just a constructor argument.

## Running increments attempts

For each message, the worker opens a scope and loads the job, node with versions, and referenced version. If any is missing, it returns. Inside try, it marks the job Running, increments Attempts, and saves. Only then does it load the blob and inspect content. The attempt count therefore measures starts at this worker stage, not successful completions or queue deliveries.

ProcessingJob.MarkRunning has no current-status guard. MarkCompleted likewise sets completion without checking a previous state, and MarkFailed chooses DeadLettered when Attempts is at least three. The entity methods are permissive transitions. The worker's orchestration determines their normal sequence, but duplicate or unexpected messages can exercise other sequences unless admission is constrained elsewhere.

The inspected worker does not skip a job merely because it is already Completed or DeadLettered. A duplicate message can call MarkRunning again and increment Attempts. This is a concrete reason to distinguish queue delivery identity from processing effect identity. A proposed idempotent consumer needs an authoritative claim or receipt, not just a status enum.

## The scanner is a bounded simulation

ReadTextSampleAsync reads at most one megabyte, resetting position when the stream can seek, and decodes those bytes as UTF-8. The worker searches the sample for the standard test signature and labels scan records with a simulated scanner name. Bytes beyond the sample are not inspected by that signature check. The implementation is a teaching simulation, not a claim of comprehensive malware detection.

Text extraction stores that sample only for selected MIME patterns. Image MIME values trigger a separate stream opening and thumbnail generation through ImageSharp. Declared MIME influences work, so a production policy would need to decide how to validate type and bound image resource consumption. The current source should be taught accurately without presenting a small test-signature check as a security guarantee.

An infected result marks the node infected, adds a scan result, marks the job completed, saves, and returns. Completed here means the processing attempt produced a terminal classification, not that the file is safe. This semantic distinction prevents dashboards from treating every completed job as a clean upload.

## Failure and retry are immediate in this path

On an exception inside the try block, the worker logs, marks the job failed or dead-lettered based on Attempts, marks the node scan failed, saves, and re-enqueues unless dead-lettered. There is no backoff delay in this catch path. A rapidly failing dependency can therefore produce closely spaced retries within the available queue scheduling.

The catch also includes cancellation-related exceptions thrown inside that region because it catches Exception broadly. It then tries to save and enqueue using the same cancellation token. If the token is cancelled, those recovery operations may themselves fail. Do not infer graceful durable shutdown from the presence of a catch block. The exact outcome depends on where cancellation is observed and which state was already saved.

Some initial lookups occur before the try block. A database exception there is not handled by the local retry catch. This is another reason to draw the actual exception boundary rather than saying the worker retries every failure. A test should identify whether failure happened before job admission, during processing, or during failure recording.

## Work three attempts

Begin with Attempts zero and Queued. The first MarkRunning makes one; failure sets Failed and re-enqueues. The second makes two; failure again sets Failed. The third makes three; failure sets DeadLettered and the catch does not enqueue again. This normal sequence expresses the intended local retry budget.

Now inject a duplicate message after completion. MarkRunning increments again because the entity and worker do not reject it in the inspected path. A design that promises at-most-one processing effect must add a claim/check boundary and handle races between consumers. A check outside a transaction can still let two workers start the same job.

Finally, fail after a thumbnail was saved but before database completion saving. Retrying may repeat that external work. The thumbnail key is based on node identity, and the exact storage adapter's write policy matters. Side effects inside a retry region need their own idempotency and cleanup analysis; a retry count does not make them transactional.

## Exercise ND-11A: trace attempt states

Write the normal three-failure sequence and a duplicate-after-completion sequence. Distinguish message delivery, attempt start, terminal classification, and clean content. Identify which current methods or worker checks prevent each invalid transition and which do not.

## Exercise ND-11B: design durable admission

Propose a database-backed claim for a queued job that supports multiple workers and recovery after a crash. Include claim expiry or another recovery mechanism, stable effect identity, and a rule for completed jobs. Explain how a channel can remain a wake-up optimization without becoming the only record of work.

## Exercise ND-11C: classify cancellation failures

Place cancellation before initial lookup, during sample reading, and after a state save. Trace which catch boundary is reached and which recovery calls use the cancelled token. Design a controlled fixture for one case and state what it does not prove about full host shutdown.

## Review checkpoint

A strong answer can explain the intended retry budget and the unprotected duplicate path at the same time. It respects the scanner's bounded simulation, identifies channel lifetime, and treats failure recording as another fallible operation. It proposes durable admission without claiming the existing row and in-memory queue already form a reliable dispatcher.
