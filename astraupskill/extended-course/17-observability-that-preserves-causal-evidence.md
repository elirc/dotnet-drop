# 17. Observability that preserves causal evidence

An incident report such as "the upload failed" is too coarse for this application. Upload receipt, chunk publication, whole-file verification, blob save, metadata commit, processing dispatch, scan completion, and response delivery happen at different times. This chapter designs diagnostic evidence around those boundaries. The proposed event vocabulary is teaching work; it is not a claim that the current application emits every event described here.

Use [ProcessingWorker](../../src/NetDropbox.Infrastructure/Processing/ProcessingWorker.cs), [JanitorWorker](../../src/NetDropbox.Infrastructure/Processing/JanitorWorker.cs), and [FileService](../../src/NetDropbox.Application/Files/FileService.cs) as anchors. Their visible logging and state transitions provide starting points, while the earlier chapters supply the upload timeline. The prerequisite is comfort distinguishing a method return from a durable business outcome.

## Begin with a question, not a log statement

Suppose support asks whether retrying completion will create another version. The useful evidence includes session identity, the completion operation's identity, its recorded state, the selected file node and version, and whether metadata commit succeeded. A generic exception message with a timestamp may not answer any of those questions. Before adding a log, write the decision that the log should support.

For a missing thumbnail, the questions differ. Was a processing message created? Did a worker start the relevant version? Was its MIME type considered an image? Did image decoding or storage fail? Did a later version overwrite node-level metadata? These are not interchangeable with upload-byte questions. A diagnostic design should allow a reviewer to follow one version through the processing path.

Correlation identifiers are necessary but insufficient. An identifier connects records; it does not tell whether a record was written before or after the operation it describes. Name events with precise boundaries: metadata-save-attempted, metadata-save-confirmed, dispatch-attempted, dispatch-returned. Avoid a single completed event emitted before several remaining effects. If a process crashes after an effect but before logging, the absence of a log remains ambiguous.

## Separate operational logs from business evidence

Logs can be dropped, sampled, delayed, or stored independently from the database. They are useful for diagnosis, but a replay protocol should not rely on a support engineer finding a log line. A durable receipt or operation ledger would be application state. Its invariant should be enforced by the persistence mechanism and used by the retry path.

The distinction matters for access counting. ShareService increments AccessCount before opening the blob. A metric named download_completed derived from that increment would overstate delivery. A more faithful name is share_download_admitted or access_recorded, depending on the chosen product definition. A separate transport observation could count bytes delivered, but client disconnects and range requests complicate what "complete" means.

Similarly, a processing job in Completed state does not prove that every later reader sees a derivative for the currently selected version. The state belongs to a job; the node metadata has its own relationship to versions. Include node identifier, version identifier, job identifier, and attempt number where the event concerns processing. This avoids treating a node identifier alone as sufficient provenance.

## Work a diagnostic timeline

Imagine an upload session S, file F, version V2, and job J. Completion saves metadata and returns a response, but the process exits before an in-memory message is consumed. A later request sees V2 pending. The diagnostic bundle should show the persisted session and job records, the absence of a processing-start observation if available, and the queue implementation's lifetime. It should not conclude that the bytes are missing merely because scanning has not started.

Now change one fact: J has Attempts equal to one and Failed status, while the node is failed. This supports a different investigation. The worker entered its attempt and encountered an exception in the handled region. The stored error category and phase can help decide whether the failure was blob read, decoding, derivative publication, or metadata save. A free-form error string alone may conflate those phases.

Change another fact: J is completed for V1, while F currently selects V2. The relevant question is not simply whether any job completed for F. The reviewer must compare version identities and ordering. A dashboard that groups only by file identifier can hide stale work. A table with one row per attempt and explicit version identity is more useful than a green icon based on the latest arbitrary event.

These timelines demonstrate why a diagnostic bundle should preserve raw facts before interpretation. Record the observed identifiers, states, timestamps, and known source behavior. Then state the hypothesis and the next discriminating observation. Avoid rewriting uncertain facts into a confident story merely because the story seems plausible.

## Design safe diagnostic fields

Do not log bearer tokens, share tokens, passwords, raw file content, or full extracted text. A support need rarely requires these values. Use internal identifiers, bounded error categories, content length, and explicitly approved metadata. Even file names can be sensitive in real deployments; decide their treatment rather than adding them casually to every error message.

High-cardinality identifiers are useful in logs but can be expensive metric labels. A counter partitioned by phase and outcome is bounded; one partitioned by every file identifier grows with user activity. Keep per-operation detail in a trace or structured log and aggregate operational health through a small vocabulary. This is a design principle for the proposed instrumentation, not an assertion about a monitoring product installed here.

Timing should distinguish elapsed duration from business timestamps. A monotonic timer is appropriate for measuring an operation's duration. UTC timestamps help correlate events across systems but can have clock skew. A negative apparent interval between hosts does not automatically prove an impossible execution. For a deterministic lesson, use an ordered event recorder and injected delays instead of treating wall-clock order as the only truth.

## Fault injection as a diagnostic rehearsal

Build a fake collaborator that fails on a named call such as blob-open or metadata-save. Have it append events to an in-memory recorder before throwing. The test should assert both the exception and the event prefix. For example, a failure before blob-open must not be reported as bytes-delivered. This can validate the intended vocabulary without running a full production stack.

Then ask what happens if the recorder itself fails. Diagnostic output should not become an undocumented second business transaction. Some audit requirements legitimately demand durable audit writes, but that requirement must be explicit and have its own failure policy. An ordinary debug log should not silently decide whether a user receives a file.

Finally, rehearse a support response. Give a reviewer a bundle with one missing observation and ask for two competing explanations. Require the next read-only check that would distinguish them. This exercise discourages the common habit of equating an absent event with an absent effect.

## Exercise ND-17A: rename misleading metrics

Define accurate names and units for share access recording, processing attempts, pending versions, and completed response transfers. For each, state the event that increments it and one conclusion it cannot support. Keep metric labels bounded.

## Exercise ND-17B: diagnose the pending version

Produce a short incident worksheet for a persisted pending version after a process restart. Include two competing hypotheses, the source facts that make them plausible, and the next read-only observations. Do not prescribe a replay until duplicate effects have been considered.

## Exercise ND-17C: design a privacy-conscious event schema

Choose fields for processing-start, processing-failure, and derivative-published events. Exclude sensitive payloads, explain identifier cardinality, and distinguish phase from outcome. Add a test that would catch an event emitted before the effect it claims to confirm.

## Review checkpoint

Useful observability narrows uncertainty. It does not manufacture certainty from a missing line or rename an attempted action as a delivered result. A strong answer keeps persisted business evidence, operational diagnostics, and user-visible outcomes separate while making their relationships easy to inspect.
