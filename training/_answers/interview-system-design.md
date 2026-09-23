# ANSWER — system design reference points (not the only valid design)

1. **Tenancy.** Put `tenant_id` on every table, with RLS or an EF global filter as a second line of defense (INC-003). A DB per tenant only for the few who pay for isolation. **Dedupe must be scoped per tenant**, and per region: `UNIQUE (tenant_id, region, content_hash)`. Cross-tenant dedupe leaks existence and breaks deletion guarantees.
2. **Uploads.** Presigned S3 multipart per region. Per-part SHA-256 (`x-amz-checksum-sha256`). Quota reserved at session creation (idempotently, per the INC-001 lesson) and settled on complete. The API handles metadata only.
3. **Processing.** An outbox to a regional broker (SQS/Service Bus/RabbitMQ) at this volume (about 15 k jobs/min across stages). Per-tenant queues or weighted fair dequeue. An autoscaled scanner fleet keyed on queue age. Poison classification and a DLQ with a replay tool.
4. **Metadata.** Partition `file_nodes`/`file_versions` by `tenant_id` (hash) or use Citus. The tree queries are all tenant-local. Search moves to OpenSearch per region, fed by CDC. "Shared-with-me" across tenants is rare; route it through a small global index.
5. **Multi-region.** Tenant-homed, active-passive per tenant (the home region is the primary for that tenant's metadata and bytes, with async replicas for DR). Active-active on a file tree means conflict resolution on moves and renames, which isn't worth it. Travelers are served from the home region through a CDN, or with presigned URLs, while data residency still holds.
6. **Identity.** OIDC/SAML per tenant through an IdP broker. Asymmetric signing (RS256/ES256) with `kid` and a JWKS, 15-minute access tokens plus refresh, and a revocation list for admin "kill session".
7. **Audit.** An append-only audit log (download, share, permission change) with user, tenant, IP, `jti`. Keep 1 year hot and 7 years cold in the tenant's region. Tenants can export it.

**Pressure-point answers.**
- Cross-tenant dedupe is a side channel (the "confirmation of a file" attack), so scope it per tenant.
- `SKIP LOCKED` is comfortable to about 1–2 k claims/s on good hardware with short transactions. Above that, or when you need fan-out, move to a broker. Measure lock waits and vacuum pressure on the jobs table first.
- Regional failure mid-upload: the session is homed in the region, so the client gets a 503 and retries later. Multipart parts in S3 persist, and the session resumes when the region returns. Don't fail over half-written sessions.
- Tenant delete: a crypto-shred per-tenant key (per-tenant SSE-KMS keys), then async object deletion and a deletion certificate. With per-tenant dedupe, nothing is shared.
- First page: "oldest un-scanned upload age" per region. It captures a queue stall, a scanner outage and a lost enqueue in one number.
