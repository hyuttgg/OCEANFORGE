using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OceanForge.Contracts.ScriptKey;

/// <summary>
/// JSON payload structure embedded with the Lua script when the user clicks "Get Script".
/// Matches specification image 02_script_lua_mau.png.
/// </summary>
public sealed class ScriptKeyPayloadDto
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// Operating system detection info matching specification image 03_thong_tin_os.png.
/// </summary>
public sealed class DetectedOsInfoDto
{
    public string OsName { get; set; } = "Windows 11";
    public string OsFamily { get; set; } = "Windows";
    public string RawDescription { get; set; } = string.Empty;
    public string Architecture { get; set; } = "X64";
    public string CpuId { get; set; } = string.Empty;
    public string Hwid { get; set; } = string.Empty;
    public string DisplayLabel => $"{OsName} ({OsFamily})";
}

/// <summary>
/// Stored license/script key entity in Database & Cache.
/// Matches specification images 04_thuat_toan_tao_key.png & 05_thuat_toan_huy_key.png.
/// </summary>
public sealed class ScriptKeyRecordDto
{
    public string Key { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string OsFamily { get; set; } = string.Empty;
    public string CpuId { get; set; } = string.Empty;
    public string Hwid { get; set; } = string.Empty;
    public string ClientIp { get; set; } = "127.0.0.1";
    public string Status { get; set; } = "active"; // "active" | "revoked" | "expired"
    public string Version { get; set; } = "1.0.0";
    public string Sha256Digest { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(24);
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
}

/// <summary>
/// Rate-limit / Anti-spam response payload matching specification image 06_chong_spam_key.png.
/// </summary>
public sealed class RateLimitErrorResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "RATE_LIMIT";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Bạn đã tạo key quá nhiều lần. Vui lòng thử lại sau 5 phút.";

    [JsonPropertyName("retry_after")]
    public int RetryAfter { get; set; } = 300;
}

/// <summary>
/// Result of key generation & Lua script preparation.
/// </summary>
public sealed class KeyGenerationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = "OK";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("retry_after")]
    public int RetryAfterSeconds { get; set; }

    public ScriptKeyPayloadDto? Payload { get; set; }
    public ScriptKeyRecordDto? Record { get; set; }
    public string RawJsonPayload { get; set; } = string.Empty;
    public string LuaLoaderLine { get; set; } = string.Empty;
    public string CombinedClipboardScript { get; set; } = string.Empty;
    public string EncryptedLuaScript { get; set; } = string.Empty;
    public string RateLimitJsonResponse { get; set; } = string.Empty;
}

/// <summary>
/// Result of key revocation matching specification image 05_thuat_toan_huy_key.png:
/// { "success": true, "message": "Key revoked" }
/// </summary>
public sealed class KeyRevocationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("revoked_at")]
    public DateTime? RevokedAtUtc { get; set; }

    public string RawResponseJson { get; set; } = string.Empty;
}

/// <summary>
/// Anti-spam status snapshot for UI telemetry & diagnostics.
/// </summary>
public sealed class AntiSpamStatusSnapshot
{
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string ClientIp { get; set; } = "127.0.0.1";
    public int RequestsInLastHour { get; set; }
    public int MaxRequestsPerHour { get; set; } = 10;
    public int DeviceCooldownSeconds { get; set; } = 300; // 5 minutes
    public int RemainingCooldownSeconds { get; set; }
    public bool IsTemporarilyLocked { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int SpamViolationCount { get; set; }
}
