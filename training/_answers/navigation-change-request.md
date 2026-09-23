# ANSWER — CR-14 placement (reference)

**1. Where.** Add a central `IFolderAccess.CanWriteAsync(userId, folderId)` (Application): true if the user owns the folder or has an Editor `DirectShare` on it **or an ancestor** (a recursive CTE, same as J2). Call sites:
- `StartUploadHandler` (`StartUpload.cs:41-49`) replaces the owner-only `folderOk` check.
- `UploadChunkHandler` and `CompleteUploadHandler` stay keyed on `session.OwnerId == request.UserId`. The *uploader* owns the session, and that check is still right.
- `CompleteUploadHandler`'s file lookup (`CompleteUpload.cs:63-66`) filters `x.OwnerId == request.UserId`. For a shared folder it must filter by the **folder owner**, or the Editor creates a duplicate-named file instead of a new version.
- `FileService.ListAsync` is owner-only (`FileService.cs:15`), so the Editor can't even see the folder contents. That needs a shared-folder listing path. (`SharedWithMeAsync` returns only top-level shared nodes.)

**2. Data.** No new share columns needed (`Role` exists). Add `UploadSession.TargetOwnerId` (the folder owner), set at start. The new `FileNode.OwnerId` is the **folder owner**: ownership follows the container, so the owner can manage and delete it. `FileVersion.UploaderId` already records the Editor (`FileVersion.cs:26`). Show it in the versions DTO (additive field).

**3. Quota.** Check and reserve against `TargetOwnerId`'s account, in the same handler that creates the version (`CompleteUploadHandler`), with the idempotent reservation from INC-001/M1. The Editor can fill the owner's quota; mention a per-share byte limit as a follow-up.

**4. Matrix changes.** Editor: start/chunk/complete ✔ (new); list the folder ✔ (new); download files in the folder ✔ (new, via the corrected INC-003 predicate *plus* ancestor shares); delete: **defer** (policy decision). Viewer: list and download ✔, upload ✘. Stranger: all ✘ (a 404, not 403).

**5. Tests.** Handler tests: `Editor_can_start_upload_into_shared_folder`; `Viewer_cannot_start_upload` (404); `Stranger_cannot_start_upload`; `Editor_upload_charges_owner_quota`; `Editor_upload_same_name_creates_new_version_for_owner_file`; `Editor_share_on_parent_applies_to_subfolder` (Testcontainers, CTE). Plus an authorization-matrix test over all FileService read methods.

**6. Risks.** `OwnerId ==` appears in `StartUpload.cs:45,52`, `CompleteUpload.cs:30,65`, `UploadChunk.cs:31`, `FileService.cs:15,36,66,98,108,116,129,145`, `ShareService.cs:13,27,35`. Every one encodes "the actor is the owner". Also, `UsageAsync` shows only the caller's usage; an Editor won't see why the owner's quota blocks them. That's a UX gap.

**7. Defer.** Editor delete/rename (needs a policy, trash ownership and an audit trail); upload via public links with `SharePermission.EditUpload` (unauthenticated writes; separate threat model); inheritance *revocation* semantics.

**Rubric.** Strong: spots the `CompleteUpload.cs:65` owner filter (a duplicate-file bug), sets ownership = folder owner with the reasoning, keeps session ownership with the uploader, calls out ancestor inheritance, and defers delete explicitly.
