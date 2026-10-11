using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OceanForge.Contracts.ScriptKey;

namespace OceanForge.Application.ScriptKey;

public interface IScriptKeyManagerService
{
    string OfficialLuaLoaderUrl { get; }
    string OfficialLuaLoaderLine { get; }

    DetectedOsInfoDto DetectCurrentOs();
    DetectedOsInfoDto ClassifyOsDescription(string rawDescription, string? cpuId = null, string? hwid = null);

    Task<KeyGenerationResult> GenerateScriptAndKeyAsync(
        string? overrideOsName = null,
        string? overrideHwid = null,
        string clientIp = "127.0.0.1",
        bool forceBypassCooldown = false,
        CancellationToken cancellationToken = default);

    Task<KeyRevocationResult> RevokeKeyAsync(
        string key,
        string reason = "User requested revocation",
        CancellationToken cancellationToken = default);

    Task<int> AutoRevokeExpiredOrMismatchedKeysAsync(
        string currentHwid,
        CancellationToken cancellationToken = default);

    string EncryptLuaSource(string plainLuaScript, string keySeed);
    AntiSpamStatusSnapshot GetAntiSpamStatus(string? hwid = null, string? osName = null, string clientIp = "127.0.0.1");
    IReadOnlyList<ScriptKeyRecordDto> GetAllKeys();
    ScriptKeyRecordDto? GetActiveCachedKey(string deviceFingerprint);
    void ResetRateLimitsForTesting();
}

public sealed class ScriptKeyManagerService : IScriptKeyManagerService
{
    public const string DefaultLuaUrl = "https://raw.githubusercontent.com/hyuttgg/kaitun/refs/heads/main/OceanForge%20desktop";
    public const string DefaultLuaLoader = "loadstring(game:HttpGet(\"https://raw.githubusercontent.com/hyuttgg/kaitun/refs/heads/main/OceanForge%20desktop\"))()";
    private const string SecretSigningKey = "OCEANFORGE-ABYSSAL-HMAC-SHA256-MASTER-SECRET-2026";
    private const string AppVersion = "1.0.0";

    // Anti-spam constants matching 06_chong_spam_key.png
    public const int DeviceCooldownSeconds = 300;        // 1 key / 5 minutes / device
    public const int MaxIpRequestsPerHour = 10;          // 10 requests / hour / IP
    public const int TemporaryLockoutSeconds = 1800;     // 30 minutes temporary lock on spam
    public const int SpamStrikeThreshold = 3;            // 3 rapid spam attempts while on cooldown triggers 30m lock

    private static readonly JsonSerializerOptions PrettyJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Database store (all historical keys) & Fast active cache
    private readonly ConcurrentDictionary<string, ScriptKeyRecordDto> _keyDatabase = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ScriptKeyRecordDto> _activeKeyCacheByKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _activeDeviceKeyCache = new(StringComparer.OrdinalIgnoreCase);

    // Rate-limiting & Anti-spam tracking stores
    private readonly ConcurrentDictionary<string, DateTime> _deviceLastGeneratedUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _deviceSpamStrikes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTime> _deviceLockedUntilUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, List<DateTime>> _ipRequestTimestampsUtc = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _rateLimitLock = new();

    public string OfficialLuaLoaderUrl => DefaultLuaUrl;
    public string OfficialLuaLoaderLine => DefaultLuaLoader;

    public DetectedOsInfoDto DetectCurrentOs()
    {
        string rawOs = RuntimeInformation.OSDescription;
        string arch = RuntimeInformation.OSArchitecture.ToString();
        string cpuId = GenerateDeterministicCpuId();
        string hwid = GenerateDeterministicHwid(cpuId);

        if (OperatingSystem.IsWindows())
        {
            Version ver = Environment.OSVersion.Version;
            string winName = ver.Major >= 10 && ver.Build >= 22000
                ? "Windows 11"
                : ver.Major >= 10
                    ? "Windows 10"
                    : ver.Major == 6 && ver.Minor >= 3
                        ? "Windows 8.1"
                        : "Windows 7";

            return new DetectedOsInfoDto
            {
                OsName = winName,
                OsFamily = "Windows",
                RawDescription = rawOs,
                Architecture = arch,
                CpuId = cpuId,
                Hwid = hwid
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return new DetectedOsInfoDto
            {
                OsName = "macOS",
                OsFamily = "macOS",
                RawDescription = rawOs,
                Architecture = arch,
                CpuId = cpuId,
                Hwid = hwid
            };
        }

        return ClassifyOsDescription(rawOs, cpuId, hwid);
    }

    /// <summary>
    /// Maps raw OS description string to the 8 supported OS targets in 03_thong_tin_os.png:
    /// Windows 11, Windows 10, Windows 8.1, Windows 7, macOS, Ubuntu, Debian, Fedora.
    /// </summary>
    public DetectedOsInfoDto ClassifyOsDescription(string rawDescription, string? cpuId = null, string? hwid = null)
    {
        string resolvedCpu = cpuId ?? GenerateDeterministicCpuId();
        string resolvedHwid = hwid ?? GenerateDeterministicHwid(resolvedCpu);
        string desc = rawDescription ?? string.Empty;

        string osName;
        string osFamily;

        if (desc.Contains("Windows 11", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Windows 11";
            osFamily = "Windows";
        }
        else if (desc.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Windows 10";
            osFamily = "Windows";
        }
        else if (desc.Contains("Windows 8.1", StringComparison.OrdinalIgnoreCase) || desc.Contains("Windows 8", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Windows 8.1";
            osFamily = "Windows";
        }
        else if (desc.Contains("Windows 7", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Windows 7";
            osFamily = "Windows";
        }
        else if (desc.Contains("Darwin", StringComparison.OrdinalIgnoreCase) || desc.Contains("macOS", StringComparison.OrdinalIgnoreCase) || desc.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
        {
            osName = "macOS";
            osFamily = "macOS";
        }
        else if (desc.Contains("Ubuntu", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Ubuntu";
            osFamily = "Linux";
        }
        else if (desc.Contains("Fedora", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Fedora";
            osFamily = "Linux";
        }
        else if (desc.Contains("Debian", StringComparison.OrdinalIgnoreCase) || desc.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            osName = "Debian";
            osFamily = "Linux";
        }
        else
        {
            osName = "Windows 11";
            osFamily = "Windows";
        }

        return new DetectedOsInfoDto
        {
            OsName = osName,
            OsFamily = osFamily,
            RawDescription = desc,
            Architecture = RuntimeInformation.OSArchitecture.ToString(),
            CpuId = resolvedCpu,
            Hwid = resolvedHwid
        };
    }

    /// <summary>
    /// Executes the 5-step Key Generation Algorithm (04_thuat_toan_tao_key.png)
    /// and 6-layer Anti-Spam Protection (06_chong_spam_key.png).
    /// </summary>
    public Task<KeyGenerationResult> GenerateScriptAndKeyAsync(
        string? overrideOsName = null,
        string? overrideHwid = null,
        string clientIp = "127.0.0.1",
        bool forceBypassCooldown = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // STEP 1: Thu thập thông tin hệ thống (OS, CPU ID, HWID, Timestamp)
        var detected = DetectCurrentOs();
        string osName = !string.IsNullOrWhiteSpace(overrideOsName) ? overrideOsName.Trim() : detected.OsName;
        string hwid = !string.IsNullOrWhiteSpace(overrideHwid) ? overrideHwid.Trim() : detected.Hwid;
        string cpuId = detected.CpuId;
        string deviceFingerprint = BuildDeviceFingerprint(hwid, osName);
        DateTime nowUtc = DateTime.UtcNow;

        // Check Anti-Spam & Rate Limits (06_chong_spam_key.png)
        if (!forceBypassCooldown)
        {
            var rateLimitViolation = CheckAndRecordRateLimit(deviceFingerprint, clientIp, nowUtc);
            if (rateLimitViolation != null)
            {
                string errorJson = JsonSerializer.Serialize(rateLimitViolation, PrettyJsonOptions);
                return Task.FromResult(new KeyGenerationResult
                {
                    Success = false,
                    Code = rateLimitViolation.Code,
                    Message = rateLimitViolation.Message,
                    RetryAfterSeconds = rateLimitViolation.RetryAfter,
                    RateLimitJsonResponse = errorJson
                });
            }
        }

        // STEP 2: Tạo chuỗi dữ liệu (Kết hợp thông tin hệ thống + Secret Key)
        string timestampIso = nowUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string nonceSalt = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        string rawDataString = $"{osName}|{cpuId}|{hwid}|{timestampIso}|{nonceSalt}|{SecretSigningKey}";

        // STEP 3: Băm và mã hóa (SHA256 + Base64 -> Tạo key dạng OF-XXXX-XXXX)
        byte[] sha256Bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawDataString));
        string sha256Hex = Convert.ToHexString(sha256Bytes).ToLowerInvariant();
        string base64Encoded = Convert.ToBase64String(sha256Bytes);
        string alphanumeric = new string(base64Encoded.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (alphanumeric.Length < 8)
        {
            alphanumeric = (alphanumeric + "8F3K7D2M9A1B").ToUpperInvariant();
        }

        string formattedKey = $"OF-{alphanumeric[..4]}-{alphanumeric.Substring(4, 4)}";

        // STEP 4: Lưu key vào Database / Cache với trạng thái Active
        if (_activeDeviceKeyCache.TryGetValue(deviceFingerprint, out string? previousKey) &&
            !string.IsNullOrWhiteSpace(previousKey) &&
            _keyDatabase.TryGetValue(previousKey, out var previousRecord) &&
            previousRecord.Status == "active")
        {
            previousRecord.Status = "revoked";
            previousRecord.RevokedAtUtc = nowUtc;
            previousRecord.RevocationReason = "Superseded by newly generated key";
            _activeKeyCacheByKey.TryRemove(previousKey, out _);
        }

        var record = new ScriptKeyRecordDto
        {
            Key = formattedKey,
            Os = osName,
            OsFamily = detected.OsFamily,
            CpuId = cpuId,
            Hwid = hwid,
            ClientIp = clientIp,
            Status = "active",
            Version = AppVersion,
            Sha256Digest = sha256Hex,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.AddHours(24)
        };

        _keyDatabase[formattedKey] = record;
        _activeKeyCacheByKey[formattedKey] = record;
        _activeDeviceKeyCache[deviceFingerprint] = formattedKey;

        // STEP 5: Trả về JSON { key, os, timestamp, version } + Lua Script
        var payload = new ScriptKeyPayloadDto
        {
            Key = formattedKey,
            Os = osName,
            Timestamp = timestampIso,
            Version = AppVersion
        };

        string rawJson = JsonSerializer.Serialize(payload, PrettyJsonOptions);
        string combinedScript = BuildCombinedLuaAndJsonScript(payload, rawJson);
        string encryptedLua = EncryptLuaSource(combinedScript, formattedKey);

        return Task.FromResult(new KeyGenerationResult
        {
            Success = true,
            Code = "OK",
            Message = "Hoàn tất! Script đã được dán vào clipboard. Bạn có thể mở Roblox và paste để sử dụng.",
            RetryAfterSeconds = 0,
            Payload = payload,
            Record = record,
            RawJsonPayload = rawJson,
            LuaLoaderLine = DefaultLuaLoader,
            CombinedClipboardScript = combinedScript,
            EncryptedLuaScript = encryptedLua
        });
    }

    /// <summary>
    /// Executes the 5-step Key Revocation Algorithm (05_thuat_toan_huy_key.png):
    /// 1. Nhận yêu cầu hủy key -> 2. Kiểm tra key trong DB/Cache -> 3. Cập nhật status = 'revoked'
    /// -> 4. Xóa khỏi Cache -> 5. Trả về kết quả { "success": true, "message": "Key revoked" }.
    /// </summary>
    public Task<KeyRevocationResult> RevokeKeyAsync(
        string key,
        string reason = "User requested revocation",
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(key))
        {
            var emptyResult = new KeyRevocationResult
            {
                Success = false,
                Message = "Key is required"
            };
            emptyResult.RawResponseJson = JsonSerializer.Serialize(new { success = false, message = emptyResult.Message }, CompactJsonOptions);
            return Task.FromResult(emptyResult);
        }

        string normalizedKey = key.Trim().ToUpperInvariant();

        // Step 2: Check key in DB / Cache
        if (!_keyDatabase.TryGetValue(normalizedKey, out var record))
        {
            var notFoundResult = new KeyRevocationResult
            {
                Success = false,
                Key = normalizedKey,
                Message = "Key not found"
            };
            notFoundResult.RawResponseJson = JsonSerializer.Serialize(new { success = false, message = "Key not found" }, CompactJsonOptions);
            return Task.FromResult(notFoundResult);
        }

        DateTime revokedAt = DateTime.UtcNow;

        // Step 3: Update status = 'revoked' and record revocation time
        record.Status = "revoked";
        record.RevokedAtUtc = revokedAt;
        record.RevocationReason = reason;

        // Step 4: Remove from active caches so it cannot be reused
        _activeKeyCacheByKey.TryRemove(normalizedKey, out _);
        string deviceFp = BuildDeviceFingerprint(record.Hwid, record.Os);
        if (_activeDeviceKeyCache.TryGetValue(deviceFp, out string? cachedKey) &&
            string.Equals(cachedKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
        {
            _activeDeviceKeyCache.TryRemove(deviceFp, out _);
        }

        // Also clear device cooldown so user can generate a fresh key if they manually revoked their compromised key
        lock (_rateLimitLock)
        {
            _deviceLastGeneratedUtc.TryRemove(deviceFp, out _);
        }

        // Step 5: Return { "success": true, "message": "Key revoked" }
        var successResponse = new
        {
            success = true,
            message = "Key revoked"
        };

        return Task.FromResult(new KeyRevocationResult
        {
            Success = true,
            Message = "Key revoked",
            Key = normalizedKey,
            RevokedAtUtc = revokedAt,
            RawResponseJson = JsonSerializer.Serialize(successResponse, PrettyJsonOptions)
        });
    }

    /// <summary>
    /// Automatically revokes expired keys or keys whose HWID no longer matches the active workstation.
    /// </summary>
    public async Task<int> AutoRevokeExpiredOrMismatchedKeysAsync(
        string currentHwid,
        CancellationToken cancellationToken = default)
    {
        int revokedCount = 0;
        DateTime nowUtc = DateTime.UtcNow;

        foreach (var kvp in _keyDatabase)
        {
            var record = kvp.Value;
            if (record.Status != "active")
            {
                continue;
            }

            if (nowUtc >= record.ExpiresAtUtc)
            {
                await RevokeKeyAsync(record.Key, "Auto-revoked: Key expired (TTL exceeded)", cancellationToken);
                revokedCount++;
            }
            else if (!string.IsNullOrWhiteSpace(currentHwid) &&
                     !string.Equals(record.Hwid, currentHwid, StringComparison.OrdinalIgnoreCase))
            {
                await RevokeKeyAsync(record.Key, "Auto-revoked: Hardware fingerprint (HWID) changed", cancellationToken);
                revokedCount++;
            }
        }

        return revokedCount;
    }

    /// <summary>
    /// Encodes/obfuscates the generated Lua script ("Mã hóa nguồn Lua") using XOR + Byte-Array loader wrapper.
    /// Protects the embedded JSON key metadata and loader URL against plain-text scraping.
    /// </summary>
    public string EncryptLuaSource(string plainLuaScript, string keySeed)
    {
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainLuaScript);
        byte[] seedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keySeed + SecretSigningKey));
        byte xorKey = (byte)((seedBytes[0] % 200) + 31); // Non-zero byte key between 31..230

        var encodedNumbers = new int[plainBytes.Length];
        for (int i = 0; i < plainBytes.Length; i++)
        {
            encodedNumbers[i] = plainBytes[i] ^ xorKey;
        }

        string byteTableLiteral = string.Join(",", encodedNumbers);
        string checksumTag = Convert.ToHexString(seedBytes)[..12];

        var sb = new StringBuilder();
        sb.AppendLine($"-- [[ OCEANFORGE PROTECTED LUA LOADER v{AppVersion} | SIG:{checksumTag} ]]");
        sb.AppendLine($"local _k={xorKey};local _b={{{byteTableLiteral}}};");
        sb.AppendLine("local _s={};for _i=1,#_b do _s[_i]=string.char(bit32.bxor(_b[_i],_k)) end;");
        sb.AppendLine("loadstring(table.concat(_s))()");
        return sb.ToString().TrimEnd();
    }

    public AntiSpamStatusSnapshot GetAntiSpamStatus(string? hwid = null, string? osName = null, string clientIp = "127.0.0.1")
    {
        var detected = DetectCurrentOs();
        string resolvedOs = !string.IsNullOrWhiteSpace(osName) ? osName : detected.OsName;
        string resolvedHwid = !string.IsNullOrWhiteSpace(hwid) ? hwid : detected.Hwid;
        string deviceFp = BuildDeviceFingerprint(resolvedHwid, resolvedOs);
        DateTime nowUtc = DateTime.UtcNow;

        lock (_rateLimitLock)
        {
            int requestsLastHour = 0;
            if (_ipRequestTimestampsUtc.TryGetValue(clientIp, out var list))
            {
                list.RemoveAll(t => (nowUtc - t).TotalSeconds > 3600);
                requestsLastHour = list.Count;
            }

            bool isLocked = false;
            DateTime? lockedUntil = null;
            int remainingCooldown = 0;

            if (_deviceLockedUntilUtc.TryGetValue(deviceFp, out DateTime lockExpiry) && lockExpiry > nowUtc)
            {
                isLocked = true;
                lockedUntil = lockExpiry;
                remainingCooldown = (int)Math.Ceiling((lockExpiry - nowUtc).TotalSeconds);
            }
            else if (_deviceLastGeneratedUtc.TryGetValue(deviceFp, out DateTime lastGen))
            {
                double elapsed = (nowUtc - lastGen).TotalSeconds;
                if (elapsed < DeviceCooldownSeconds)
                {
                    remainingCooldown = (int)Math.Ceiling(DeviceCooldownSeconds - elapsed);
                }
            }

            _deviceSpamStrikes.TryGetValue(deviceFp, out int strikes);

            return new AntiSpamStatusSnapshot
            {
                DeviceFingerprint = deviceFp,
                ClientIp = clientIp,
                RequestsInLastHour = requestsLastHour,
                MaxRequestsPerHour = MaxIpRequestsPerHour,
                DeviceCooldownSeconds = DeviceCooldownSeconds,
                RemainingCooldownSeconds = remainingCooldown,
                IsTemporarilyLocked = isLocked,
                LockedUntilUtc = lockedUntil,
                SpamViolationCount = strikes
            };
        }
    }

    public IReadOnlyList<ScriptKeyRecordDto> GetAllKeys()
    {
        return _keyDatabase.Values
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToList();
    }

    public ScriptKeyRecordDto? GetActiveCachedKey(string deviceFingerprint)
    {
        if (_activeDeviceKeyCache.TryGetValue(deviceFingerprint, out string? key) &&
            !string.IsNullOrWhiteSpace(key) &&
            _activeKeyCacheByKey.TryGetValue(key, out var record))
        {
            return record;
        }
        return null;
    }

    public void ResetRateLimitsForTesting()
    {
        lock (_rateLimitLock)
        {
            _deviceLastGeneratedUtc.Clear();
            _deviceSpamStrikes.Clear();
            _deviceLockedUntilUtc.Clear();
            _ipRequestTimestampsUtc.Clear();
        }
    }

    private RateLimitErrorResponseDto? CheckAndRecordRateLimit(string deviceFingerprint, string clientIp, DateTime nowUtc)
    {
        lock (_rateLimitLock)
        {
            // Rule 5: Check if device is under 30-minute temporary lockout
            if (_deviceLockedUntilUtc.TryGetValue(deviceFingerprint, out DateTime lockedUntil) && lockedUntil > nowUtc)
            {
                int retryLock = Math.Max(1, (int)Math.Ceiling((lockedUntil - nowUtc).TotalSeconds));
                return new RateLimitErrorResponseDto
                {
                    Success = false,
                    Code = "RATE_LIMIT",
                    Message = $"Thiết bị đang bị khóa tạm thời 30 phút do spam key liên tục. Vui lòng thử lại sau {retryLock} giây.",
                    RetryAfter = retryLock
                };
            }

            // Rule 2: IP rate limit (10 requests / hour)
            if (!_ipRequestTimestampsUtc.TryGetValue(clientIp, out var ipTimestamps))
            {
                ipTimestamps = [];
                _ipRequestTimestampsUtc[clientIp] = ipTimestamps;
            }

            ipTimestamps.RemoveAll(t => (nowUtc - t).TotalSeconds > 3600);
            if (ipTimestamps.Count >= MaxIpRequestsPerHour)
            {
                // Lock for 30 minutes on IP flood
                _deviceLockedUntilUtc[deviceFingerprint] = nowUtc.AddSeconds(TemporaryLockoutSeconds);
                return new RateLimitErrorResponseDto
                {
                    Success = false,
                    Code = "RATE_LIMIT",
                    Message = "Bạn đã vượt quá giới hạn 10 yêu cầu / giờ trên địa chỉ IP này. Hệ thống tạm khóa 30 phút.",
                    RetryAfter = TemporaryLockoutSeconds
                };
            }

            // Rule 1: Device rate limit (1 key / 5 minutes / device)
            if (_deviceLastGeneratedUtc.TryGetValue(deviceFingerprint, out DateTime lastGenerated))
            {
                double elapsedSeconds = (nowUtc - lastGenerated).TotalSeconds;
                if (elapsedSeconds < DeviceCooldownSeconds)
                {
                    int strikes = _deviceSpamStrikes.AddOrUpdate(deviceFingerprint, 1, (_, c) => c + 1);
                    if (strikes >= SpamStrikeThreshold)
                    {
                        // Escalate to 30-minute temporary lock
                        _deviceLockedUntilUtc[deviceFingerprint] = nowUtc.AddSeconds(TemporaryLockoutSeconds);
                        return new RateLimitErrorResponseDto
                        {
                            Success = false,
                            Code = "RATE_LIMIT",
                            Message = "Phát hiện hành vi spam tạo key liên tục! Thiết bị đã bị khóa tạm thời 30 phút.",
                            RetryAfter = TemporaryLockoutSeconds
                        };
                    }

                    int remaining = Math.Max(1, (int)Math.Ceiling(DeviceCooldownSeconds - elapsedSeconds));
                    // Return the canonical rate limit error from 06_chong_spam_key.png (retry_after: 300 or remaining)
                    return new RateLimitErrorResponseDto
                    {
                        Success = false,
                        Code = "RATE_LIMIT",
                        Message = "Bạn đã tạo key quá nhiều lần. Vui lòng thử lại sau 5 phút.",
                        RetryAfter = remaining > 0 ? remaining : DeviceCooldownSeconds
                    };
                }
            }

            // Record valid generation
            _deviceLastGeneratedUtc[deviceFingerprint] = nowUtc;
            _deviceSpamStrikes[deviceFingerprint] = 0;
            ipTimestamps.Add(nowUtc);
            return null;
        }
    }

    private static string BuildCombinedLuaAndJsonScript(ScriptKeyPayloadDto payload, string prettyJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- OceanForge Desktop — Auto-Generated Key & OS Configuration");
        sb.AppendLine("getgenv().OceanForgeKeyData = [[");
        sb.AppendLine(prettyJson);
        sb.AppendLine("]]");
        sb.AppendLine($"getgenv().OceanForgeKey = \"{payload.Key}\"");
        sb.AppendLine($"getgenv().OceanForgeOS = \"{payload.Os}\"");
        sb.AppendLine(DefaultLuaLoader);
        return sb.ToString().TrimEnd();
    }

    private static string BuildDeviceFingerprint(string hwid, string osName)
    {
        string raw = $"{hwid.Trim().ToUpperInvariant()}::{osName.Trim().ToUpperInvariant()}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"FP-{Convert.ToHexString(hash)[..16]}";
    }

    private static string GenerateDeterministicCpuId()
    {
        string rawCpu = $"{RuntimeInformation.ProcessArchitecture}-{Environment.ProcessorCount}C-{Environment.MachineName}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawCpu));
        return $"CPU-{Convert.ToHexString(hash)[..8]}";
    }

    private static string GenerateDeterministicHwid(string cpuId)
    {
        string rawMachine = $"{Environment.MachineName}|{Environment.UserName}|{cpuId}|{RuntimeInformation.OSArchitecture}";
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawMachine));
        return $"HWID-{Convert.ToHexString(hash)[..12]}";
    }
}
