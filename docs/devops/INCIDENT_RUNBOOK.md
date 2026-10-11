# Incident Runbook

## Severity Levels & Response SLA

| Level | Definition | Examples | SLA Response | SLA Mitigation | Action Lead |
|---|---|---|---|---|---|
| **SEV-1** | Critical security compromise, severe data loss, corrupted updater distribution | Secret leak in release, updater pushes unsigned binary, database wipe | Immediate (<15 min) | <2 hours | Lead Architect + Security Lead |
| **SEV-2** | Major system outage, widespread startup crashes, authentication service failure | Desktop crash loop on launch >5% users, backend API unavailable | <30 min | <4 hours | Release Owner + Backend Lead |
| **SEV-3** | Non-critical feature failure with available workaround | Single UI panel rendering bug, telemetry queue lag, localized cache error | <4 hours | Next patch / sprint | Component Owner |
| **SEV-4** | Minor cosmetic defect or minor log anomaly | UI alignment issue, non-blocking warning log, typo in docs | <24 hours | Next scheduled release | Assigned Developer |

---

## Response Procedures

### 1. SEV-1 Response (Critical Emergency)
1. **Declare Incident:** Notify team lead and open an incident communication channel.
2. **Contain Blast Radius:**
   - For compromised secrets: immediately revoke and rotate credentials.
   - For corrupt update/binary: pull release manifest and delete draft/assets.
   - For backend failure: cut ingress traffic or switch to maintenance page.
3. **Preserve Forensics:** Save workflow run logs, system event logs, and package hashes (redact secrets).
4. **Remediate:** Deploy hotfix or restore snapshot under Incident Lead supervision.
5. **Post-Mortem:** Conduct root cause analysis within 48 hours and publish preventative action items.

### 2. SEV-2 Response (Major Outage)
1. **Declare Incident:** Assign Incident Commander and Lead Engineer.
2. **Execute Rollback:** Refer to `docs/devops/ROLLBACK_RUNBOOK.md`. Freeze new releases.
3. **Verify Restoration:** Confirm crash rate returns below baseline threshold (<0.1%).
4. **Issue Public Notice:** Update status page or GitHub Discussions if users are affected.

---

## Hotfix Procedure
When a critical fix cannot wait for the standard release cycle:
```text
1. Branch from production release tag:
   git checkout -b fix/issue-description v1.0.0

2. Implement minimal scope fix + add regression test in:
   tests/OceanForge.Application.Tests/ or tests/OceanForge.Domain.Tests/

3. Validate locally:
   dotnet test --configuration Release

4. Open PR targeting main (or release branch) with [HOTFIX] prefix.

5. Require expedited CODEOWNER approval + CI green.

6. Merge PR and tag patch release (e.g. v1.0.1):
   git tag -a v1.0.1 -m "fix: resolve critical issue (v1.0.1)"
   git push origin v1.0.1

7. Execute CI Release workflow, verify signed assets, and publish.
```
