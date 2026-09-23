# Change request CR-14: "Upload into a shared folder" (Editor role)

**From the PM.** "Teams want to drop files into a folder a colleague shared with them. Today `DirectShareRole.Editor` exists (`src/NetDropbox.Domain/Enums.cs:32-36`) but does nothing. If I'm an Editor on a shared folder, I should be able to upload into it. Files I upload there count against the **folder owner's** quota. The owner sees me as the uploader in the version history."

**Your job, before any agent writes code.** Write `training/navigation/CR-14-placement.md` covering:
1. **Where the rule lives.** Which files and methods change? Name them. (Upload start, chunk, complete; is the share inherited by subfolders?)
2. **Data changes.** Does `direct_shares` need anything new? Who is `FileNode.OwnerId` for a file an Editor uploads into A's folder? What does `FileVersion.UploaderId` already give you?
3. **Quota.** Whose `UserStorageAccount` is checked and reserved, and in which handler? How does this interact with INC-001's lesson and with M4?
4. **Authorization matrix.** For owner / Editor / Viewer / stranger: start upload, upload chunk, complete, list the folder, download the result, delete the result. Which cells change?
5. **Tests.** List the tests by name and layer.
6. **Risks.** What existing code assumes `session.OwnerId == file.OwnerId`? (Search for `OwnerId ==` across `Application`.)
7. **What you'd refuse or defer** in the first PR, and why.

Timebox: 45 minutes. Then compare with `training/_answers/navigation-change-request.md`.
