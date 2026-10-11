# Rollback Runbook

## 1. Desktop Application Rollback

### Trigger Conditions
- Startup crash loop affecting >1% of active clients.
- Critical authentication failure or severe UI blocking bug.
- Corruption or unhandled failure in local SQLite storage.
- Signature mismatch or auto-update distribution failure.

### Procedure
1. **Halt Rollout:**
   - Immediately pause rollout in updater distribution channels.
   - Freeze the update manifest endpoint to prevent further client downloads.
2. **Select Rollback Target:**
   - Select the immediate prior stable release (`vX.Y.(Z-1)`).
   - Confirm compatibility: prior version must be compatible with current local SQLite schema and remote backend API contracts.
3. **Validate Rollback Assets:**
   - Re-verify SHA-256 hash and digital signature of the prior stable release artifact.
4. **Publish Rollback Manifest:**
   - Point updater manifest back to prior stable package version.
   - Deploy signed manifest to updater feed.
5. **Preserve User Data:**
   - Ensure rollback package does NOT purge user local data (`%LOCALAPPDATA%\OceanForge` on Windows or `~/.local/share/OceanForge` on Linux).
   - Preserve SQLite database files (`*.db`, `*.db-wal`) and outbox queues.
6. **Notify & Document:**
   - Post release incident notice with downgrade instructions for manual installs.
   - Prepare forward-fix patch (`vX.Y.(Z+1)`). Never reuse or overwrite existing git tags.

---

## 2. Backend & API Rollback

### Trigger Conditions
- HTTP 5xx error rate >2% for >5 minutes post-deployment.
- Telemetry ingestion channel latency >500ms or backpressure drop rate >1%.
- Unhandled database locks or failed schema migration step.

### Procedure
1. **Halt Traffic Promotion:**
   - Freeze Canary / Staging-to-Production traffic shifts.
   - Re-route traffic to previous healthy backend replica/target group.
2. **Container Image Rollback:**
   - If database schema is unchanged or backwards-compatible:
     - Redeploy the prior verified container image digest immediately.
     - Verify `/health/live` and `/health/ready` endpoints return HTTP 200.
3. **Database Migration Incident Handling:**
   - **Do NOT run destructive down-migrations** (`DROP TABLE`, `DROP COLUMN`) on active production data.
   - If migration succeeded but new code is broken: keep updated schema if non-breaking (expand phase) and redeploy prior API image.
   - If migration failed partially:
     - Halt API traffic.
     - Evaluate whether forward-fix script can resolve state.
     - If database is corrupt or inconsistent: restore from verified pre-migration point-in-time snapshot.
4. **Verify Backend Recovery:**
   - Confirm API error rate drops below 0.1%.
   - Verify telemetry ingestion queue drains normally without dropped frames.
   - Check MySQL active connections and slow query log.
5. **Post-Incident Actions:**
   - Record incident timeline, blast radius, and root cause in incident log.
