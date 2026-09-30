# 20. Cancellation, streams, and resource ownership

Cancellation is a request to stop work, not a transaction rollback. This distinction becomes concrete in [ProcessingWorker](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs), [LocalBlobStorage](../../src/NetDropbox.Infrastructure/Storage/LocalBlobStorage.cs), and the download methods in [FileService](../../src/NetDropbox.Application/Files/FileService.cs). The chapter develops an ownership ledger for streams and a phase-aware cancellation policy. It does not change the application or claim that every method observes a supplied token.

The prerequisite is the completion timeline from chapter 03. Once an external effect has occurred, a later cancellation cannot make it unoccur. The caller may stop waiting while the server still needs to preserve recovery evidence. This creates two questions: whether further work should continue, and who owns cleanup or reconciliation for effects that already happened.

## A token parameter is not an observation

Read each implementation rather than assuming that accepting a CancellationToken makes every operation interruptible. Local file existence and deletion use synchronous filesystem calls. OpenRead returns a stream from File.OpenRead. Copy operations pass a token into asynchronous copying. These are different observation points, and a cancellation request can arrive between them.

For a deterministic experiment, arrange a stream that blocks on a gate before its next read. Cancel while the copy is waiting, then release the gate according to the stream's implementation. Record which operation observes cancellation and which artifacts already exist. A test that cancels before calling the method may cover only the earliest path and miss partial effects.

Do not assert that cancellation always leaves no file. The local adapter creates its final path before copying completes. A canceled copy can therefore require an explicit cleanup or verification policy. The chunk adapter's pending-file strategy has different aftermath. Again, the abstraction name does not transfer one implementation's cleanup behavior to another.

## Ownership follows the call graph

A method receiving a stream must define whether it borrows the stream or takes ownership. Borrowing means the caller remains responsible for disposal; taking ownership means the callee must dispose on every relevant path. Returning a stream transfers a resource obligation to the caller or framework. Without that contract, tests can pass while handles leak or callers unexpectedly lose reusable streams.

Build an ownership table for a download. The service opens a blob stream and returns it with metadata. The controller returns a File result. Framework result execution then reads the stream and is responsible for the result's lifecycle under its contract. A direct service test should dispose the returned stream itself. It cannot rely on a controller or framework that it never invoked.

For processing, the worker opens a stream to read a text sample and may open another stream for image decoding. Each opened resource has an independent lifetime. If the sample succeeds but the image operation fails, the first stream's disposal should not be assumed to dispose the second. Read the using scopes and test disposal with a tracking stream when designing a focused fixture.

## Work a cancellation timeline

Imagine a worker has marked a job running and saved that state. It opens the blob and begins reading. The host requests shutdown. A cancellation exception enters the worker's catch region, which attempts to mark the job and node failed and save using the same token. If that token is already canceled, recovery persistence may itself fail. The source should not be described as guaranteeing a durable failure record for every shutdown.

This is not an argument to ignore shutdown forever. A proposed policy can separate stopping new work from completing a short bookkeeping step under a bounded independent timeout. Another policy can leave a running claim for a recovery process to reclaim later. The choice depends on whether durable work identity and recovery scanning exist. Without those mechanisms, swallowing cancellation can make the system less understandable rather than safer.

Now consider a public download. AccessCount is saved before the blob is opened. Cancellation after that save does not reverse the count. A client that receives no bytes may still have a recorded access. If the product wants a completed-transfer count, it needs a different event and must define partial or ranged downloads. Cancellation exposes an existing semantic choice; it does not create it.

## Avoid cleanup that hides the original failure

A cleanup operation can fail too. Suppose copying throws and deleting a temporary artifact also throws. Which exception should the caller receive, and where is the cleanup obligation recorded? Replacing the original copy exception with an unrelated cleanup exception can destroy useful causal evidence. Suppressing cleanup failure entirely can hide a persistent orphan.

A teaching harness can record both failures in a structured result: primary operation failure and cleanup failure. Production code may use logging, an aggregate exception, or a durable cleanup record depending on the API contract. The important requirement is that the second failure does not silently erase the first and that remaining artifacts are not forgotten.

Cleanup should also be idempotent where retries are expected. Deleting an already absent temporary artifact can be success. Releasing the same quota reservation twice should not subtract twice. Disposing a resource more than once may be supported by a particular type, but do not use that as a substitute for clear ownership. Each cleanup action needs semantics suited to its resource.

## Design cancellation tests around phases

Use a table with phases: before admission, during byte receipt, after chunk publication, after blob publication, after metadata commit, during processing, and during response transfer. For each phase, record what is durable, what can still be stopped, what the caller sees, and what recovery owes. The table prevents a single vague requirement such as "cancellation cleans up" from hiding contradictory expectations.

A unit fixture with fakes can prove that a token is forwarded and that later collaborators are not called after an early failure. A filesystem fixture can prove the artifact aftermath for a local adapter. A hosted streaming fixture can observe response behavior after headers begin. A restart test with a database can evaluate recovery. These layers should be selected by the claim, not by a desire to run every available suite.

Keep the first runnable experiment small. A tracking stream can expose reads, disposal, and a controlled exception without external dependencies. A manual scheduler can place cancellation between two named events. This is more reproducible than timing a large real upload and hoping a cancellation lands at the interesting point.

## Exercise ND-20A: draw the ownership ledger

List the owner of request body, opened blob stream, text-sample stream, image stream, and returned download stream at each boundary. Identify where a direct test must dispose a resource that a hosted request would normally delegate to the framework. Mark any contract question requiring further source inspection.

## Exercise ND-20B: choose a shutdown policy

Compare bounded bookkeeping after cancellation with recoverable running claims. State the prerequisites, failure modes, and test evidence for each. Explain why reusing an already canceled token for recovery persistence cannot establish a guaranteed saved failure record.

## Exercise ND-20C: inject a cleanup failure

Design a fake copy operation and fake cleanup operation that both fail. Specify the reported primary cause, the recorded cleanup obligation, and a retry that does not repeat a logical release. Include a test showing that cleanup failure remains visible without replacing the original operation's identity.

## Review checkpoint

Cancellation handling is complete only when resource ownership and surviving effects are explicit. A well-structured answer can say what stops, what remains, and who reconciles it. It does not promise rollback from a token or treat a successful method return as proof that every later consumer finished reading.
