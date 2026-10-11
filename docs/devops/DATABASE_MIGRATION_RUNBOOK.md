# Database Migration Runbook (MySQL 8.4 LTS)

## Policy & Strategy
All backend database schema migrations target MySQL 8.4 LTS.
- **Pattern:** Expand/Contract design. Breaking changes must span across at least two releases:
  1. *Expand:* Add new columns/tables as nullable or with defaults. Old and new code both function.
  2. *Backfill:* Populate new columns asynchronously.
  3. *Switch:* Deploy code reading/writing new columns.
  4. *Contract:* Remove deprecated columns/tables in a subsequent release after verification.
- **Client Independence:** Desktop clients must never connect directly to MySQL. All migrations apply only to backend services.

## Migration Gate Procedures

### 1. Pre-Deployment Verification (CI & PR Gate)
- Migration SQL files placed in `database/migrations/` with sequential numbering (`V001__init.sql`).
- PR review required by designated Database Codeowner.
- Automated migration run against clean MySQL 8.4 container in CI.
- Automated migration upgrade run against synthetic dataset representing production schema.
- DDL lock duration evaluated (no exclusive table locks exceeding 2 seconds).

### 2. Pre-Migration Execution Checklist (Production)
- [ ] Fresh automated backup verified within the last 60 minutes.
- [ ] Backup point-in-time recovery metadata recorded.
- [ ] Compatible API versions confirmed running or staged for immediate promotion.
- [ ] Off-peak execution window verified.
- [ ] Migration executed via dedicated migration job using `MYSQL_MIGRATION_USER` (least privilege, separate from API runtime credentials).

### 3. Execution & Verification Steps
1. Execute migration runner job against target environment.
2. Monitor lock waits and replication lag:
   ```sql
   SHOW PROCESSLIST;
   SELECT * FROM performance_schema.data_locks;
   ```
3. Run post-migration schema verification query:
   ```sql
   SELECT table_name, table_rows, avg_row_length 
   FROM information_schema.tables 
   WHERE table_schema = DATABASE();
   ```
4. Verify application health endpoints and error metrics.

### 4. Failure & Recovery Procedures
- If migration fails during execution:
  - Immediately stop deployment pipeline.
  - Do NOT run uncontrolled rollback DDL scripts on production.
  - Assess state: if transaction rolled back cleanly, maintain current API version.
  - If state is inconsistent: initiate restore from pre-migration backup to clean staging/production clone before retrying.
