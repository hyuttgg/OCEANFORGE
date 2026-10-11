#!/usr/bin/env bash
set -euo pipefail

MYSQL_HOST="${MYSQL_HOST:-127.0.0.1}"
MYSQL_PORT="${MYSQL_PORT:-3306}"
MYSQL_USER="${MYSQL_USER:-oceanforge_test_user}"
MYSQL_PWD="${MYSQL_PASSWORD:-test-only-password}"
MYSQL_DB="${MYSQL_DATABASE:-oceanforge_test}"
export MYSQL_PWD

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MIGRATIONS_DIR="$SCRIPT_DIR/../migrations"

for sql_file in "$MIGRATIONS_DIR"/*.sql; do
  echo "Applying migration: $(basename "$sql_file")"
  mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$MYSQL_DB" < "$sql_file"
done

echo "Verifying applied migrations:"
mysql -h "$MYSQL_HOST" -P "$MYSQL_PORT" -u "$MYSQL_USER" "$MYSQL_DB" -e "SELECT version, applied_at_utc FROM schema_migrations ORDER BY version;"
