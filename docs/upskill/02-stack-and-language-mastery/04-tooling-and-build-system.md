# Tooling And Build System

| Tool | Evidence | What to learn |
| --- | --- | --- |
| MSBuild shared props | [Directory.Build.props](../../../Directory.Build.props#L1-L9) | One file controls target framework and nullable behavior for all projects. |
| API project refs | [NetDropbox.Api.csproj](../../../src/NetDropbox.Api/NetDropbox.Api.csproj#L1-L18) | API references Application and Infrastructure, not Domain directly. |
| Application packages | [NetDropbox.Application.csproj](../../../src/NetDropbox.Application/NetDropbox.Application.csproj#L1-L11) | MediatR and FluentValidation belong here. |
| Vite scripts | [package.json](../../../web/package.json#L6-L10) | `build` runs TypeScript then Vite bundling. |
| CI | [ci workflow](../../../.github/workflows/ci.yml#L8-L32) | Build/test split mirrors local commands. |

Verified commands are in [command cheatsheet](../08-reference/command-cheatsheet.md). Do not add packages casually: every dependency changes supply-chain risk, restore time, and the team’s maintenance obligations.

