# Netdrop extended upload and recovery course

This complete instructional route extends the [existing Astra pack](../README.md) with 21 source-grounded chapters, 63 independent exercises, seven separate reviews, three capstone briefs, and two disposable current-source labs. It exceeds the requested 31,920 new instructional prose words; README navigation and fenced code are excluded from that count.

Read in order, make predictions before running a lab, and consult the corresponding review afterward. Current application behavior and proposed capstone behavior are explicitly distinguished. Application source, dependencies, configuration, and earlier lessons remain unchanged.

## Learning route

Chapters 01-07 establish upload bytes, publication, completion, resume identity, stream evidence, and the first compatible lab. Chapters 08-14 examine versions, folders, sharing, processing, cleanup, persistence, and HTTP boundaries. Chapters 15-17 deepen storage contracts, changing read models, and diagnostics. Chapters 18-20 develop independent capstones and resource ownership. Chapter 21 closes with current-source lifecycle characterization and a final evidence ladder.

## Chapters

- [01. Derive the byte contract before moving data](01-derive-the-byte-contract.md)
- [02. Publication is not the same as receiving](02-publication-is-not-the-same-as-receiving.md)
- [03. Completion has several commit boundaries](03-completion-has-several-commit-boundaries.md)
- [04. First capstone: a recoverable completion ledger](04-capstone-a-recoverable-completion-ledger.md)
- [05. Resume identity and expired open sessions](05-resume-identity-and-expired-open-sessions.md)
- [06. Stream position, length, and content evidence](06-stream-position-length-and-content-evidence.md)
- [07. A domain lab and a final-chunk counterexample](07-domain-lab-and-a-final-chunk-counterexample.md)
- [08. Version history is not scan history](08-version-history-is-not-scan-history.md)
- [09. Folder graphs, names, and operation-specific guards](09-folder-graphs-names-and-operation-specific-guards.md)
- [10. Share capabilities and download boundaries](10-share-capabilities-and-download-boundaries.md)
- [11. Processing attempts and queue lifetime](11-processing-attempts-and-queue-lifetime.md)
- [12. Janitor work and accounting obligations](12-janitor-work-and-accounting-obligations.md)
- [13. Persistence metadata is not a concurrency proof](13-persistence-metadata-is-not-a-concurrency-proof.md)
- [14. HTTP validation and error evidence](14-http-validation-and-error-evidence.md)
- [15. Local and object storage are different implementations of an incomplete contract](15-local-and-object-storage-contracts.md)
- [16. Search results are a changing read model](16-search-results-and-changing-read-models.md)
- [17. Observability that preserves causal evidence](17-observability-that-preserves-causal-evidence.md)
- [18. Capstone: make processing results belong to a version](18-capstone-version-aware-processing.md)
- [19. Capstone: quota and retention as accountable obligations](19-capstone-quota-retention-and-reconciliation.md)
- [20. Cancellation, streams, and resource ownership](20-cancellation-streams-and-resource-ownership.md)
- [21. Lifecycle characterization and a final demonstration](21-lifecycle-characterization-and-final-demonstration.md)

## Separate solutions and rubrics

- [Review 01: bytes, publication, and recovery](review-01-bytes-publication-and-recovery.md)
- [Review 02: resume and stream evidence](review-02-resume-and-stream-evidence.md)
- [Separate lab review](review-03-domain-lab.md)
- [Review 04. Versions, folders, shares, and jobs](review-04-versions-folders-shares-and-jobs.md)
- [Review 05. Cleanup, persistence, HTTP, and storage](review-05-cleanup-persistence-http-and-storage.md)
- [Review 06. Read models, diagnostics, and capstone assessment](review-06-read-models-diagnostics-and-capstones.md)
- [Review 07. Lifecycle lab answers](review-07-lifecycle-lab.md)

## Running the bounded labs

The [chunk-domain lab](07-domain-lab-and-a-final-chunk-counterexample.md) and [lifecycle lab](21-lifecycle-characterization-and-final-demonstration.md) copy selected current domain source into explicit new external directories. Each has reference, intentionally broken, and copied-file repair workflows. Neither starts the API or requires a database or cloud account. Python and the .NET 10 SDK must already be available.

The labs prove their listed domain observations only. Database concurrency, hosted authorization, filesystem crash recovery, and remote-object durability require the separately described integration experiments. Capstone features are learner proposals, not changes delivered to the application. Keep these evidence limits in every submission.
