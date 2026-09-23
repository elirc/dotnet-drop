# J0 — Make `main` build and boot

**Context.** A fresh clone of `main` doesn't compile. Once you get it compiling, the first DB call throws while the EF model is being built. Nobody noticed, because the author's machine had the missing files on disk.

**Ticket.** "Fresh clone → `dotnet build` → `docker compose up` must work. Fix whatever stops it, in the smallest possible diff."

**Constraints.** Don't change behavior. Don't commit build output. Runtime `storage/` and `chunks/` data must still stay out of git.

**Definition of done.** Clone into an empty folder, then `dotnet build src/NetDropbox.Api/NetDropbox.Api.csproj` succeeds. A small test proves `new NetDropboxDbContext(options).Model` builds without a database. The CI workflow in `.github/workflows/ci.yml` would pass.

**Explain before touching.**
1. Which files are missing from git, and which exact line makes git ignore them? Prove it with one git command.
2. Why is this invisible on the author's machine but fatal in CI (`ubuntu-latest`)? Careful: Linux is case-sensitive. Would the ignore rule even match there, and does that matter?
3. Where does the EF model fail, and why do two configuration calls conflict?
4. What test stops each regression from coming back?

<details><summary>Hints</summary>

- `git check-ignore -v <path>` and `git ls-files src/NetDropbox.Infrastructure`.
- Anchor ignore rules to where runtime data is actually written (content root = `src/NetDropbox.Api/`). Then check that `Infrastructure/Storage/*.cs` can be added.
- `HasMany(x => x.Versions)` plus `UsePropertyAccessMode(PropertyAccessMode.Field)`.
</details>

Answer: `training/_answers/ladder-J0.md`
