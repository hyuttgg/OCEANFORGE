# ADR-002: Release Package Formats and Runtime Strategy

## Status
Accepted

## Context
OceanForge Desktop targets Windows (10 21H2+, 11) and Linux (Ubuntu 22.04/24.04 LTS, Debian 12) using Avalonia UI 12.1.4 and .NET 10 LTS (`net10.0`). We require a distribution and packaging strategy that ensures high runtime reliability across target machines while avoiding missing runtime prerequisites or reflection-related startup crashes.

## Decision
1. **Windows Distribution:**
   - **Primary (Initial):** Portable ZIP archive containing self-contained binaries (`OceanForge-<version>-win-x64-portable.zip`).
   - **Secondary / Target (When signing ready):** Signed MSIX package (`.msix`) once Authenticode certificates and trusted publisher pipeline are established.
2. **Linux Distribution:**
   - **Primary:** Native Debian package (`.deb`) structured under `packaging/linux/deb-root/` targeting Ubuntu 22.04/24.04 LTS and Debian 12.
   - **Secondary:** Portable tarball (`OceanForge-<version>-linux-x64-portable.tar.gz`) for non-Debian environments and portable execution.
   - AppImage packaging is deferred until automated containerized build and smoke tests are validated.
3. **Runtime Deployment Model:**
   - **Self-Contained (`--self-contained true`):** All release binaries bundle the required .NET 10 LTS runtime per target Runtime Identifier (`win-x64`, `linux-x64`). End users are not required to install the .NET SDK or runtime separately.
4. **Trimming and Native AOT Strategy:**
   - **No Trimming (`PublishTrimmed=false`):** Disabled to prevent stripping types required by Avalonia UI XAML loaders, CommunityToolkit.Mvvm reflection, and dynamic telemetry channels.
   - **No Native AOT (`PublishAot=false`):** Disabled until Avalonia and all third-party dependencies are certified for full AOT compatibility.
   - **Single File (`PublishSingleFile=false`):** Disabled initially to facilitate straightforward symbol debugging, file extraction, and predictable asset loading.

## Consequences
- **Package Size:** Artifact sizes will be larger (~80–120 MB per archive/package) due to bundling the .NET 10 runtime.
- **Zero Prerequisite Burden:** End users can run OceanForge immediately without manual .NET runtime installations.
- **Deterministic Stability:** Eliminates reflection, trimming, and dynamic code generation failures in production.
- **Gradual Hardening:** Allows progressive evolution towards signed MSIX and AppImage without blocking initial release milestones.
