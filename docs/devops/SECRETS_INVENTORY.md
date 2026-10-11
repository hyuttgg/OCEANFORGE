# Secrets and Variables Inventory

All secrets and variables must be configured in GitHub Repository or GitHub Environment settings. Desktop packages never receive database credentials.

| Name | Type | Scope / Environment | Description | Status |
|---|---|---|---|---|
| `STAGING_API_BASE_URL` | Variable | Environment: `staging` | Staging API endpoint URL | NOT CREATED |
| `PRODUCTION_API_BASE_URL` | Variable | Environment: `production` | Production API endpoint URL | NOT CREATED |
| `WINDOWS_SIGNING_CERT` | Secret | Environment: `production-release` | Base64-encoded Authenticode/MSIX signing certificate (PFX) | NOT CREATED |
| `WINDOWS_SIGNING_PASSWORD` | Secret | Environment: `production-release` | Password for Windows signing certificate | NOT CREATED |
| `CLOUD_OIDC_ROLE_ARN` | Variable | Environment: `staging`, `production` | AWS/Cloud IAM role ARN for short-lived OIDC authentication | NOT CREATED |
| `STAGING_DEPLOY_ROLE` | Variable | Environment: `staging` | Role name or identity used for staging deployments | NOT CREATED |
| `PRODUCTION_DEPLOY_ROLE` | Variable | Environment: `production` | Role name or identity used for production deployments | NOT CREATED |
| `MYSQL_HOST` | Secret | Environment: `staging`, `production` | Backend MySQL 8.4 database host | NOT CREATED |
| `MYSQL_PORT` | Variable | Environment: `staging`, `production` | Backend MySQL 8.4 database port (default: 3306) | NOT CREATED |
| `MYSQL_DATABASE` | Secret | Environment: `staging`, `production` | Backend database schema name | NOT CREATED |
| `MYSQL_MIGRATION_USER` | Secret | Environment: `staging`, `production` | Dedicated DB user for schema migration jobs | NOT CREATED |
| `MYSQL_MIGRATION_PASSWORD` | Secret | Environment: `staging`, `production` | Dedicated DB password for schema migration jobs | NOT CREATED |
| `PACKAGE_FEED_TOKEN` | Secret | Repository | Read-only access token for private package feeds (if needed) | NOT CREATED |

## Rules
1. **Desktop Client Protection:** Database credentials (`MYSQL_*`) are strictly reserved for backend deployment jobs. Never embed DB secrets into desktop binaries or configuration.
2. **No Command-Line Leaks:** Never pass secrets as command-line arguments in CI steps. Use environment variables.
3. **No Secret Dumps:** Workflow logs must not echo environment variables or dump configuration files.
