# Branch and PR Policy

## Branching Strategy
Trunk-based development centered on `main`.

| Branch | Purpose | Lifecycle |
|---|---|---|
| `main` | Production-ready source trunk. Direct pushes blocked. | Permanent |
| `feature/<short-desc>` | New features | Short-lived, delete on merge |
| `fix/<short-desc>` | Bug fixes | Short-lived, delete on merge |
| `security/<short-desc>` | Security patches | Short-lived, delete on merge |
| `refactor/<short-desc>` | Refactoring without behavioral change | Short-lived, delete on merge |
| `release/<version>` | Release stabilization branch (if needed) | Tagged and archived |

> Note: Existing git branch `master` must be renamed to `main` (`git branch -m master main`).

## Pull Request Rules
1. **PR Mandatory:** Direct push to `main` is strictly prohibited.
2. **Review Gate:** Minimum 1 approval from a CODEOWNER/maintainer. Stale reviews dismissed on new pushes.
3. **Branch Freshness:** Branch must be up to date with `main` before merge.
4. **CI Gate (Green Required):**
   - Solution restore with locked mode (`dotnet restore --locked-mode`)
   - Code format verification (`dotnet format --verify-no-changes`)
   - Build Release configuration on `windows-latest` and `ubuntu-latest`
   - Unit tests pass with TRX reporting (`dotnet test --no-build`)
5. **No Broken Changes:** Merges require linear history or squash-and-merge with conventional commit message.

## Commit Conventions
Follow Conventional Commits:
- `feat:` New feature
- `fix:` Bug fix
- `perf:` Performance enhancement
- `security:` Security fix or hardening
- `ci:` CI/CD workflow changes
- `build:` Build scripts or dependency updates
- `refactor:` Code reorganization without functional changes
- `docs:` Documentation updates
- `chore:` Housekeeping tasks
