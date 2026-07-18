# Maintainer Communication

Good question template:

> I traced X through `[anchor]` and found Y. I think options are A or B. I prefer A because of risk Z. Am I missing a constraint?

Bad question:

> What should I do?

Bug report template:

- Reproduction.
- Expected.
- Actual.
- Logs/correlation id.
- First narrowed layer: client, API, application, persistence, worker.

Respectful disagreement: cite code and risk, not preference. Example: "I prefer an outbox here because [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L82-L84) commits DB state before queue delivery is durable."

