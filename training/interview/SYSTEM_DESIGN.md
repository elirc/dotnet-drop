# System design: take NetDropbox to 50× scale, multiple regions, enterprise tenants

**Today (from code):** one API process with an in-process worker and janitor; Postgres; a local or S3 blob store; chunks on local disk; an in-memory queue; per-user ownership with no tenants; one JWT HS256 key.

**Prompt (45 min, whiteboard).** "Acme and 40 other enterprises want NetDropbox. Targets: 50× today's traffic (assume 5 k uploads/min at peak, 20 k downloads/min, 2 PB stored), EU data must stay in the EU, SSO, per-tenant admin, audit logs, 99.95 % availability."

Cover, in this order:
1. **Tenancy model.** Add `tenant_id` everywhere, or a schema per tenant, or a DB per tenant? What happens to the dedupe design (`blobs.content_hash` is globally unique today) across tenants, and in the EU versus the US?
2. **Upload path at 5 k/min.** Keep chunk-through-API or presigned multipart? Where are the checksum and quota enforced?
3. **Processing.** A queue choice, a fairness strategy and the scanner fleet. How do you keep a single tenant's bulk import from starving the others?
4. **Metadata DB.** What gets partitioned or sharded, and on what key? Which queries become cross-shard (search, shared-with-me)?
5. **Multi-region.** Active-active or active-passive? Where does a user's file-tree metadata live? What does "download" do for an EU user traveling in the US?
6. **Identity.** Moving from one HS256 key to OIDC/SAML per tenant: key rotation, `kid`, token lifetimes, revocation.
7. **Observability and audit.** Who downloaded what, retained for how long, stored where?

## Pressure points the interviewer will push on
- "Your dedupe spans tenants. Tenant A can learn that tenant B has a file with hash X by uploading it and seeing it complete instantly. Is that a leak? What do you change?"
- "The jobs table is in Postgres. At 5 k uploads/min × 3 stages, is `SKIP LOCKED` still fine? What number makes you switch, and to what?"
- "An EU tenant shares a link with a US partner. Where are the bytes served from? What does the DPA say?"
- "A region fails mid-upload. Chunks 0–40 are in eu-west-1. What happens to the session?"
- "Deleting a tenant: how long does it take, how do you prove it's done, and what about deduped blobs shared with other tenants?"
- "What single metric would you page on first?"

Reference points are in `training/_answers/interview-system-design.md`.
