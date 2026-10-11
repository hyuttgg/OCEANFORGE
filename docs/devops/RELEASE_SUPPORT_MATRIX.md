# Release Support Matrix

Validation ledger for supported target operating systems, architectures, and package distributions.

> Note: Status remains `NOT RUN` until automated smoke tests or manual QA verification logs are recorded for a specific release candidate. Never change `NOT RUN` to `PASS` without verified execution evidence.

| Operating System | Version | Architecture | Package Format | Clean Install | Upgrade | Uninstall | Status |
|---|---|---|---|---|---|---|---|
| **Windows** | 11 (23H2 / 24H2) | x64 | Portable ZIP | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Windows** | 11 (23H2 / 24H2) | x64 | MSIX (planned) | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Windows** | 10 (21H2+) | x64 | Portable ZIP | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Windows** | 10 (21H2+) | x64 | MSIX (planned) | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Ubuntu** | 24.04 LTS (Noble) | x64 | `.deb` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Ubuntu** | 24.04 LTS (Noble) | x64 | Portable `tar.gz` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Ubuntu** | 22.04 LTS (Jammy) | x64 | `.deb` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Ubuntu** | 22.04 LTS (Jammy) | x64 | Portable `tar.gz` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Debian** | 12 (Bookworm) | x64 | `.deb` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |
| **Debian** | 12 (Bookworm) | x64 | Portable `tar.gz` | NOT RUN | NOT RUN | NOT RUN | NOT RUN |

## Verification Criteria
- **Clean Install:** Application extracts/installs cleanly, creates necessary directories, and launches without error on clean OS images.
- **Upgrade:** Application updates successfully over previous stable version while preserving user settings and SQLite data.
- **Uninstall:** Uninstaller removes binaries and shortcuts while keeping or cleanly prompting for user data retention.
