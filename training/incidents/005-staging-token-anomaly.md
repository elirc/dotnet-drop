# INC-005 — Everyone logged out, then a forged-token report

**Severity:** SEV-1 once confirmed (authentication)
**Branch:** `training/incidents/005-staging-token-anomaly`

## Timeline as on-call saw it

- **Tue 14:05.** PR #227 deploys to staging, then production at 14:40.
- **Tue 14:06–14:50.** `401` on `/api/v1/*` jumps from 0.2 % to 71 % for about 8 minutes on each environment, then falls back to 0.3 %. Support gets "I got kicked out and had to sign in again." On-call decides it was "probably a token cache flush from the deploy" and closes the alert.
- **Fri 10:15.** Bug bounty:

> Your API accepts JWTs I sign myself. I built this token with the HS256 key I found in a public GitHub mirror of your repo, with `nameid = a11ce...` (an ID I saw in a share-link JSON response). `GET /api/v1/files` returned that user's root folder. It works on `api.netdropbox.example` and `staging-api...`.

## Evidence

```
Tue 14:06:12 INF HTTP GET /api/v1/files responded 401 in 3 ms
Tue 14:06:12 INF   WWW-Authenticate: Bearer error="invalid_token", error_description="The signature key was not found"
Tue 14:06:40 INF HTTP POST /api/v1/auth/login responded 200 in 118 ms
Tue 14:06:41 INF HTTP GET /api/v1/files responded 200 in 22 ms
```

Production deployment manifest (unchanged for four months):

```yaml
env:
  ASPNETCORE_ENVIRONMENT: Production
  ConnectionStrings__Postgres: { secretRef: pg-conn }
  Jwt__Issuer: NetDropbox
  Jwt__Audience: NetDropboxClient
  Jwt__SigningKey: { secretRef: jwt-signing-key }   # 64 random bytes, rotated yearly
  Storage__Provider: S3
```

No errors or warnings appear at startup. `/healthz` is green throughout.

## The suspect PR (#227, "no behaviour change", CI green)

> **refactor(auth): move JWT settings under Authentication section.** Groups auth config under "Authentication" ahead of the OIDC work next quarter.

```diff
--- a/src/NetDropbox.Api/Program.cs
+++ b/src/NetDropbox.Api/Program.cs
-var jwt = builder.Configuration.GetSection("Jwt");
+// All auth settings now live under "Authentication" (JWT today, OIDC next quarter).
+var jwt = builder.Configuration.GetSection("Authentication:Jwt");
 var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"] ?? "local-development-signing-key-change-me-32chars"));
--- a/src/NetDropbox.Api/Controllers/AuthController.cs
+++ b/src/NetDropbox.Api/Controllers/AuthController.cs
-        var jwt = configuration.GetSection("Jwt");
+        var jwt = configuration.GetSection("Authentication:Jwt");
--- a/src/NetDropbox.Api/appsettings.json
+++ b/src/NetDropbox.Api/appsettings.json
-  "Jwt": {
-    "Issuer": "NetDropbox",
-    "Audience": "NetDropboxClient",
-    "SigningKey": "local-development-signing-key-change-me-32chars"
-  },
+  "Authentication": {
+    "Jwt": {
+      "Issuer": "NetDropbox",
+      "Audience": "NetDropboxClient",
+      "SigningKey": "local-development-signing-key-change-me-32chars"
+    }
+  },
```

## Your tasks

1. Explain Tuesday's 401 spike and Friday's report as **one** mechanism. Trace how ASP.NET Core configuration resolves `Authentication:Jwt:SigningKey` in Production, layer by layer.
2. The PR said "no behaviour change" and CI was green. List every place in this repo where the *old* key path still appears. Which of them would a reviewer have seen in the diff?
3. There are two independent defects that turned a rename into a SEV-1. One was in the PR. The other has been in `main` since the first commit. Name both.
4. Write the fix, plus a startup guard that would have made Tuesday's deploy fail loudly. Then write the test for the guard.
5. Incident response: what must be rotated or invalidated, in what order? How do you find forged-token requests after the fact with the logging this app has? What would you add?
