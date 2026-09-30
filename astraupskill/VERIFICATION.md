# Verification evidence and practical limits

The final accepted Netdrop test run is sol15-netdrop-tests-r4. It completed with exit code zero and twenty-two passing tests, with none skipped. The recorded command, working directory, environment overrides, timestamps, and complete output are linked below after packaging. It uses the campaign's isolated .NET 8 executable, one MSBuild worker, disabled build-server reuse, and the previously restored dependency graph. Running the test project compiles the Domain, Application, Infrastructure, and test assemblies involved in this change.

The delivered implementation strengthens upload declaration validation, expected chunk lengths, accepted-file publication, temporary-file lifetime, assembled byte accounting, quota preflight, and the FileNode version relationship mapping. Source review inspected the ordering of external storage and database mutations and identified the overwrite race that a sequential duplicate test alone would miss. The real filesystem regression now exercises competing different bodies. The early oversized-stream check bounds the read and temporary write to the declared chunk boundary instead of consuming an arbitrary request body.

Evidence must be interpreted by provider. Domain tests are pure object tests. Filesystem tests use actual temporary files and their publication semantics on this Windows host. Handler tests exercise actual handlers and the application EF model, but persist into EF InMemory and substitute external chunk, blob, and queue ports. Fresh contexts make persisted fixture state observable; they do not turn InMemory into PostgreSQL or establish database uniqueness, transaction rollback, isolation, or a multi-process race guarantee.

No original application database was migrated, reset, or seeded for these checks. No live blob provider or processing service was called. The API host, frontend, and a deployed end-to-end upload were not part of the accepted test command. Post-database queue delivery and content-addressed blob reconciliation remain separate operational boundaries. The implementation does not blindly delete a shared blob when database persistence fails. Earlier failed attempts remain available in campaign logs, and snapshots preserve the original contents of changed baseline files. The appended evidence records only the final passing run, avoiding duplicate test totals from retries. Reproduce the command in an isolated checkout before extending the claims to another storage provider or deployment environment.

## Source excerpt

From [tests/NetDropbox.Tests/NetDropbox.Tests.csproj](../tests/NetDropbox.Tests/NetDropbox.Tests.csproj).

```csproj
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\NetDropbox.Domain\NetDropbox.Domain.csproj" />
    <ProjectReference Include="..\..\src\NetDropbox.Application\NetDropbox.Application.csproj" />
    <ProjectReference Include="..\..\src\NetDropbox.Infrastructure\NetDropbox.Infrastructure.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.11" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="3.10.0" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

## Course navigation

[README](README.md) / [01-CODEBASE-MAP](01-CODEBASE-MAP.md) / [02-CONCEPTS](02-CONCEPTS.md) / [03-WORKED-CHANGE](03-WORKED-CHANGE.md) / [04-TESTING-AND-DEBUGGING](04-TESTING-AND-DEBUGGING.md) / [05-PRACTICE](05-PRACTICE.md) / [06-SOLUTIONS-AND-REVIEW](06-SOLUTIONS-AND-REVIEW.md) / [07-TRACE-LAB](07-TRACE-LAB.md) / [VERIFICATION](VERIFICATION.md)

## Recorded command evidence

The recorded test runs passed **22 tests**. The commands below define the verified scope; counts refer to tests, not commands.

| Check | Exact command arguments | Exit | Evidence |
|---|---|---:|---|
| `sol15-netdrop-tests-r4` | `["C:\\Users\\Owner\\Desktop\\AstraOrganized2\\astra-remaining-improvements\\toolchains\\dotnet8\\dotnet.exe", "test", "tests\\NetDropbox.Tests\\NetDropbox.Tests.csproj", "--disable-build-servers", "-m:1", "--no-restore"]` | 0 | [record](evidence/sol15-netdrop-tests-r4.json), [log](evidence/sol15-netdrop-tests-r4.log) |
