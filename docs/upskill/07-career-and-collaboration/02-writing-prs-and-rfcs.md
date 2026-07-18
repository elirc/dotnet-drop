# Writing PRs And RFCs

## PR Template

- What changed?
- Why?
- How tested? Include commands from [command cheatsheet](../08-reference/command-cheatsheet.md).
- Risk and rollback.
- Screenshots or API examples if UI/API changed.

## RFC Template

Use for outbox, search, auth changes, schema changes.

1. Problem.
2. Current behavior with anchors, for example [CompleteUploadHandler.Handle](../../../src/NetDropbox.Application/Uploads/CompleteUpload.cs#L28-L86).
3. Options considered.
4. Decision.
5. Migration and rollback.
6. Tests and observability.

