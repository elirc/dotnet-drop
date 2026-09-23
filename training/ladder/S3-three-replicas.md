# S3 — Run three replicas in the cloud, with rollback

**Scenario.** Sales closed a customer that requires 99.9 % availability. Today NetDropbox is one container (`docker-compose.yml` `api`) with local volumes for `/app/storage` and `/app/chunks`. You must run **three API replicas** behind a load balancer on AWS (ECS/Fargate + RDS + S3) **or** Azure (Container Apps + Flexible Server + Blob Storage). Pick one and justify it.

**State inventory to start from (verify each against code):**
- Chunks on local disk (`FileSystemChunkStorage`, `Chunks:Root`). The client uploads chunks in parallel (`web/src/App.tsx:285-288`), so consecutive chunks of one upload can land on **different replicas**.
- The blob store is Local or S3 (`DependencyInjection.cs:33-53`). `S3CompatibleBlobStorage.SaveAsync` checks the bucket exists on every call.
- In-memory `Channel` queue plus `ProcessingWorker` in every replica.
- `JanitorWorker` in every replica, sweeping the same rows every 15 minutes.
- Rate limiting is per-process and in-memory, per IP (`Program.cs:60-71`).
- `EnsureCreated` and seeding are guarded only by the environment name.

**Deliverable (≤ 2 pages).**
1. For each stateful item above: what breaks with three replicas, and your fix (sticky sessions? S3 multipart upload for chunks? presigned URLs? a leader lock for the janitor?).
2. **Uploads:** should chunk bytes keep flowing through the API at all? Compare pass-through with presigned S3 multipart, covering the checksum verification, quota enforcement and cost trade-offs.
3. **Deploy pipeline:** build once and promote by digest; migrations as a separate step (S2); health and readiness checks (`/healthz` today only checks Postgres); blue/green or rolling; and **rollback** that is compatible with the schema.
4. **Config and secrets:** where the JWT key, DB credentials and S3 credentials live. How startup fails if one is missing (see INC-005).
5. **Observability:** the three dashboards and five alerts you'd create on day one.
6. **Cost:** a rough monthly estimate for 3 × 1 vCPU/2 GB, a managed Postgres with an HA standby, and 20 TB of object storage. What dominates?

**Explain before touching.** What happens today, step by step, if chunk 0 goes to replica A, chunk 1 to replica B, and `complete` to replica C?

Answer: `training/_answers/ladder-S3.md`
