# ANSWER — J0 make main build and boot

**Q1.** Missing: `src/NetDropbox.Infrastructure/Storage/{FileSystemChunkStorage,LocalBlobStorage,S3CompatibleBlobStorage}.cs`. The line is `.gitignore:8`, `storage/`. Proof: `git check-ignore -v src/NetDropbox.Infrastructure/Storage/LocalBlobStorage.cs` prints `.gitignore:8:storage/`. Also `git ls-files src/NetDropbox.Infrastructure` lists no Storage files.

**Q2.** The author's disk has the files, so their build passes, and on Windows `core.ignorecase=true` makes `storage/` match `Storage/`, so `git status` never showed them as untracked. On Linux CI the ignore rule would *not* match `Storage/`, but that doesn't matter: the files were never committed, so CI checks out a tree without them and `DependencyInjection.cs:31,45,52` fails with CS0246. Lesson: "ignored" and "missing" look identical in a clean `git status`.

**Q3.** `NetDropboxDbContext.cs:44` `HasMany<FileVersion>("_versions")` creates a navigation named `_versions` backed by that field. `:48` `b.Navigation(x => x.Versions).UsePropertyAccessMode(Field)` configures a second navigation, `Versions`, whose backing field by convention is also `_versions`. EF throws: *"The member 'FileNode._versions' cannot use field '_versions' because it is already used by 'FileNode.Versions'."* Fix: `b.HasMany(x => x.Versions).WithOne().HasForeignKey(x => x.FileNodeId)...` and keep line 48. (The uncommitted Astra diff makes exactly this change.)

**Fix for the ignore rule:** replace `storage/` and `chunks/` with anchored paths, e.g. `/src/NetDropbox.Api/storage/`, `/src/NetDropbox.Api/chunks/` (and `/storage/`, `/chunks/` at the root). Then `git add src/NetDropbox.Infrastructure/Storage`.

**Q4 tests.**
- `ModelBuildsTest`: `new NetDropboxDbContext(new DbContextOptionsBuilder<>().UseNpgsql("Host=unused").Options).Model` must not throw. It needs no database, is fast, and catches every mapping error.
- CI builds `NetDropbox.Api.csproj` from a clean checkout, which already exists. It just never ran. Add `git ls-files --others --ignored --exclude-standard -- '*.cs'` as a CI or pre-commit step that fails if any `.cs` file is ignored.

**Rubric.** Solid means you found both issues and proved the first with git. Strong means you also explained why the Linux case doesn't save you, and added the "ignored source file" guard.
