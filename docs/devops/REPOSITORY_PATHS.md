# Repository Paths Inventory (`REPOSITORY_PATHS.md`)

Verified paths in the repository:

```text
Solution path: OceanForge.slnx
Desktop project: src/OceanForge.Desktop/OceanForge.Desktop.csproj
Domain project: src/OceanForge.Domain/OceanForge.Domain.csproj
Contracts project: src/OceanForge.Contracts/OceanForge.Contracts.csproj
Application project: src/OceanForge.Application/OceanForge.Application.csproj
AI project: src/OceanForge.AI/OceanForge.AI.csproj
Telemetry project: src/OceanForge.Telemetry/OceanForge.Telemetry.csproj
Local persistence project: src/OceanForge.Local.Persistence/OceanForge.Local.Persistence.csproj
API project: NOT CREATED (modular monolith local HTTP bridge in OceanForge.Telemetry)
Worker project: NOT CREATED
Unit test project(s):
  - tests/OceanForge.Domain.Tests/OceanForge.Domain.Tests.csproj
  - tests/OceanForge.Application.Tests/OceanForge.Application.Tests.csproj
Windows package script: packaging/windows/package-win-x64.ps1
Linux package script: packaging/linux/package-deb.sh
Linux Debian control root: packaging/linux/deb-root/
Updater manifest schema: packaging/update-manifest.schema.json
Updater manifest generator: packaging/generate-update-manifest.ps1
Database migrations: database/migrations/001_initial_schema.sql
Database migration verifier: database/scripts/verify-migrations.sh
```
