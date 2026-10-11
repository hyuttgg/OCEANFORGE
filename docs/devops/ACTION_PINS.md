# GitHub Actions Pin Ledger (`ACTION_PINS.md`)

All external GitHub Actions in `.github/workflows/*.yml` are pinned by full-length commit SHA per GitHub security hardening policy.

| Action | Version Tag | Full Commit SHA | Workflows |
|---|---|---|---|
| `actions/checkout` | `v4.2.2` | `11bd71901bbe5b1630ceea73d27597364c9af683` | `ci.yml`, `security.yml`, `nightly.yml`, `desktop-release.yml` |
| `actions/setup-dotnet` | `v4.3.1` | `67a3573c9a986a3f9c594539f4ab511d57bb3ce9` | `ci.yml`, `security.yml`, `nightly.yml`, `desktop-release.yml` |
| `actions/upload-artifact` | `v4.6.2` | `ea165f8d65b6e75b540449e92b4886f43607fa02` | `ci.yml`, `nightly.yml`, `desktop-release.yml` |
| `actions/download-artifact` | `v4.3.0` | `d3f86a106a0bac45b974a628896c90dbdf5c8093` | `desktop-release.yml` |
| `github/codeql-action/init` | `v3.28.11` | `45775bd8235c68ba998cffa5171334d58593da47` | `security.yml` |
| `github/codeql-action/analyze` | `v3.28.11` | `45775bd8235c68ba998cffa5171334d58593da47` | `security.yml` |
| `actions/dependency-review-action` | `v4.5.0` | `3c4e3ceb1aa6dadf6d4bf82fa1fbdf2e9a4d5c9a` | `security.yml` |
| `anchore/sbom-action` | `v0.18.0` | `e22c389904149ebc2ce5fb0ebbf2afddfe753e8e` | `desktop-release.yml` |
| `actions/attest-build-provenance` | `v2.2.3` | `c07444fd9c96c29fb7b7d7b89c1415c956205d3f` | `desktop-release.yml` |
