# ANSWER — INC-005 JWT config rename → public signing key in production

## One mechanism
Production supplies the real key as the environment variable `Jwt__SigningKey`, which maps to config key `Jwt:SigningKey`. PR #227 made both `Program.cs` and `AuthController` read `Authentication:Jwt:SigningKey`. Configuration layers are appsettings.json → appsettings.{Env}.json → env vars → command line. For the new key, the only layer with a value is **appsettings.json**, which now contains the committed dev key under `Authentication:Jwt`. Even without that entry, both call sites fall back to the same hard-coded string (`?? "local-development-signing-key-change-me-32chars"`, `Program.cs:42`, `AuthController.cs:32`).

- **Tuesday:** tokens issued before the deploy were signed with the vault key. The new pods validate with the dev key → `invalid_token` / key not found → 401 → users log in again → new tokens signed with the dev key → everything "works". The alert self-resolved, which is the most dangerous kind.
- **Friday:** the dev key is public (repo, `.env.example:5`, `docker-compose.yml:60`, `appsettings.json:8`), so anyone can mint HS256 tokens with any `nameid`. `CurrentUser.UserId()` trusts the claim.

## Old key path still referenced (the reviewer never saw these)
`docker-compose.yml:58-60` (`Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey`), `.env.example:3-5`, the production manifest, and `docs/upskill` command references. None are in the diff. A rename of a config key is a **contract change with the deployment**, and the deployment lives outside the diff.

## The two defects
1. **(PR)** The config path was renamed without migrating deployment configuration or supporting the old key.
2. **(main since day one)** A **silent fallback to a known key** in two places, plus a real-looking key committed in `appsettings.json`. Secrets must fail closed. If the key had been required, Tuesday's deploy would have crashed at startup, which is a SEV-4 caught by the rollout health check.

## Fix + guard
```csharp
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Authentication:Jwt"))
    .Validate(o => o.SigningKey is { Length: >= 32 }, "Jwt signing key missing/short")
    .Validate(o => builder.Environment.IsDevelopment() || o.SigningKey != DevKey, "Dev JWT key outside Development")
    .ValidateOnStart();
```
- Use one `JwtOptions` for issuer and validator. Today they read config separately and can drift.
- Move the dev key to `appsettings.Development.json` or user-secrets only.
- For the transition, either keep reading `Jwt` as a fallback section, or ship the manifest change **first** (expand), then the code (contract).

**Test:** `WebApplicationFactory<Program>` with `ASPNETCORE_ENVIRONMENT=Production` and no key → assert host startup throws `OptionsValidationException`. A second case: only `Jwt__SigningKey` set → assert startup succeeds and a token signed with the dev key is rejected (401).

## Response order
1. Deploy the fix with the real key read correctly. That instantly invalidates every dev-key token, including forged ones and all legitimate sessions since Tuesday. Accept the forced re-login.
2. Rotate the vault key anyway (a new key, in case the manifest was also leaked) and add `kid` headers so future rotations can overlap.
3. Forensics: forged tokens are indistinguishable from real ones by signature (same key). Look for tokens whose `nameid` never logged in: `auth/login` success rows vs requests. The app doesn't log user ID per request (`UseSerilogRequestLogging`, not enriched) and has no login audit table, so answer honestly: *we can't fully scope access.* Add per-request `UserId`, `jti`, `iat` logging and a login audit table.
4. Notify affected tenants per policy.

## Postmortem lesson
"No behaviour change" refactors of config keys are deployment changes. A self-resolving 401 spike right after an auth-adjacent deploy is a signal to investigate, not noise.
