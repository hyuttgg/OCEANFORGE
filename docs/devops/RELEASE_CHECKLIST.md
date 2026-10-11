# Release Checklist

## Phase 1: Pre-Tag Gate (Before Creating Tag)
- [ ] Release scope finalized and all planned PRs merged into `main`.
- [ ] No uncommitted or local manual changes.
- [ ] Working tree clean on `main` branch.
- [ ] CI workflow on `main` is green (build, unit tests, formatting).
- [ ] CodeQL, dependency audit, and secret scanning checks passed.
- [ ] Backend API compatibility and MySQL migrations reviewed (if applicable).
- [ ] UI smoke tests passed on supported target platforms (`win-x64`, `linux-x64`).
- [ ] SemVer tag determined following standard convention (`vX.Y.Z`).
- [ ] `docs/project/CHANGELOG_WORK.md` and release notes drafted.

## Phase 2: Tag & Build Gate (Post-Tag / CI Execution)
- [ ] Annotated tag pushed to remote (`git tag -a vX.Y.Z -m "OceanForge vX.Y.Z" && git push origin vX.Y.Z`).
- [ ] CI release pipeline triggered on exact tag commit SHA.
- [ ] Self-contained packages built for `win-x64` and `linux-x64`.
- [ ] Packaging outputs verified (no debug configuration or test credentials embedded).
- [ ] Authenticode/MSIX code signing completed and verified (Windows).
- [ ] Package signatures and permissions verified (Linux).
- [ ] `checksums-sha256.txt` generated from final signed package files.
- [ ] `SBOM.spdx.json` generated for build commit.
- [ ] Build provenance / artifact attestation completed.
- [ ] GitHub Draft Release created with candidate assets.

## Phase 3: Pre-Publish Stable Gate (Approval & Verification)
- [ ] Draft release assets downloaded to clean test machines (Windows 11 / Ubuntu 24.04).
- [ ] SHA-256 checksums verified independently against `checksums-sha256.txt`.
- [ ] Clean install test passed.
- [ ] Upgrade install test from previous stable release passed.
- [ ] Application launch, authentication, and core telemetry ingestion smoke test passed.
- [ ] Rollback rehearsal completed on staging.
- [ ] Update manifest schema verified (valid URLs, real SHA-256 hashes, exact file sizes).
- [ ] Designated release maintainer approved the production deployment.

## Phase 4: Post-Publish Gate (Post-Release Monitoring)
- [ ] Release published on GitHub (status changed from Draft to Public Release).
- [ ] Update manifest deployed to production feed.
- [ ] Clean install tested from public production URL.
- [ ] Real-time monitoring verified (startup crash rate, API error rates, telemetry stream).
- [ ] Previous release artifacts preserved for emergency rollback.
- [ ] Any post-release anomalies documented in `docs/project/KNOWN_ISSUES.md`.
- [ ] Release retrospective scheduled (within 3 business days).
