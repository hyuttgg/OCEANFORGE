# Environment Matrix

| Parameter | Development (Local) | Staging | Production |
|---|---|---|---|
| **Purpose** | Developer workstations & local iteration | Integration testing, migration drills, QA verification | Live end-user distribution and services |
| **Hosting / Runner** | Windows 11 / Linux local machines | GitHub Actions / Staging Cloud / Staging Server | GitHub Releases / Production Cloud / Production Server |
| **Desktop Runtime** | .NET 10.0.401, Avalonia UI 12.1.4 (Debug / Release) | Self-contained package candidate (win-x64, linux-x64) | Signed final packages (win-x64, linux-x64) |
| **Desktop Database** | Local SQLite (`OceanForge.Local.Persistence`) | Local SQLite (fresh + upgrade test databases) | Local SQLite (managed user app data directory) |
| **Backend Database** | Local / Docker MySQL 8.4 LTS (or mocked) | Dedicated MySQL 8.4 LTS instance (synthetic/masked data) | Clustered MySQL 8.4 LTS (automated backup, point-in-time recovery) |
| **Backend Services** | Local API / Worker dev instances | Staging container deployment via GitHub OIDC | Production container deployment via GitHub OIDC |
| **Network Endpoints** | `http://localhost:*` | `https://staging-api.oceanforge.local` | `https://api.oceanforge.net` |
| **Secrets & Keys** | Local `appsettings.Development.json` (gitignored), mock keys | GitHub Environment `staging` secrets & variables | GitHub Environment `production` and `production-release` secrets |
| **Deployment Gate** | Direct local build / run | Automatic on PR merge to `main` | Manual approval required, protected tag (`v*.*.*`) |
| **Rollback Support** | Git branch / stash reset | Automated container rollbacks, synthetic DB reset | Fast rollback manifest, immutable container digest, verified DB backups |
