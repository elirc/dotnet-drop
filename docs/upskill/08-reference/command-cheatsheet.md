# Command Cheatsheet

| Command | Status | Use |
| --- | --- | --- |
| `git rev-parse HEAD` | Verified fail | Shows this working tree has no commit to pin. |
| `git log --oneline -30` | Verified fail | Shows no organic commit history. |
| `rg --files` | Verified pass | Inventory. |
| `& "$HOME\.dotnet\dotnet.exe" build src/NetDropbox.Api/NetDropbox.Api.csproj --no-restore` | Verified pass | Backend build. |
| `& "$HOME\.dotnet\dotnet.exe" test tests/NetDropbox.Tests/NetDropbox.Tests.csproj --no-restore` | Verified pass | Test suite. |
| `npm run build` from `web/` | Verified pass | Frontend TypeScript/Vite build. |
| `& "$HOME\.dotnet\dotnet.exe" list src/NetDropbox.Api/NetDropbox.Api.csproj package --vulnerable --include-transitive` | Verified pass | Package vulnerability check. |
| `docker compose up --build` | Inferred | Documented run command, not executed in this pass. |

See [verification log](verification-log.md) for trimmed outputs.

