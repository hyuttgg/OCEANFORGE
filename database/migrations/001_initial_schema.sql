-- Migration ID: 001_initial_schema
-- Owner: @hyuttgg
-- Schema change: Initial users, user_identities, script_keys, and sync_outbox tables (MySQL 8.4 LTS)
-- Backfill required: No
-- Compatible app/API versions: >= 0.1.0
-- Dangerous operation?: No (additive CREATE TABLE IF NOT EXISTS)

CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(64) NOT NULL PRIMARY KEY,
    applied_at_utc DATETIME(6) NOT NULL DEFAULT (UTC_TIMESTAMP(6))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS users (
    id CHAR(36) NOT NULL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(128) NOT NULL,
    avatar_url VARCHAR(512) NULL,
    email_verified TINYINT(1) NOT NULL DEFAULT 0,
    status VARCHAR(32) NOT NULL DEFAULT 'active',
    created_at_utc DATETIME(6) NOT NULL,
    updated_at_utc DATETIME(6) NOT NULL,
    last_login_at_utc DATETIME(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS user_identities (
    id CHAR(36) NOT NULL PRIMARY KEY,
    user_id CHAR(36) NOT NULL,
    provider VARCHAR(32) NOT NULL,
    provider_user_id VARCHAR(128) NOT NULL,
    provider_email VARCHAR(255) NOT NULL,
    linked_at_utc DATETIME(6) NOT NULL,
    last_used_at_utc DATETIME(6) NOT NULL,
    UNIQUE KEY uq_provider_identity (provider, provider_user_id),
    CONSTRAINT fk_identities_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS script_keys (
    key_code VARCHAR(32) NOT NULL PRIMARY KEY,
    os_name VARCHAR(64) NOT NULL,
    os_family VARCHAR(32) NOT NULL,
    architecture VARCHAR(16) NOT NULL,
    hwid_hash CHAR(64) NOT NULL,
    status VARCHAR(16) NOT NULL DEFAULT 'active',
    created_at_utc DATETIME(6) NOT NULL,
    revoked_at_utc DATETIME(6) NULL,
    INDEX idx_script_keys_hwid_status (hwid_hash, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

INSERT IGNORE INTO schema_migrations (version) VALUES ('001_initial_schema');
