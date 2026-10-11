using System.Text.Json;
using System.Threading.Tasks;
using OceanForge.Application.ScriptKey;
using OceanForge.Contracts.ScriptKey;
using Xunit;

namespace OceanForge.Application.Tests;

public class ScriptKeyManagerServiceTests
{
    [Theory]
    [InlineData("Microsoft Windows 11 Pro 10.0.22631", "Windows 11", "Windows")]
    [InlineData("Microsoft Windows 10 Enterprise 10.0.19045", "Windows 10", "Windows")]
    [InlineData("Microsoft Windows 8.1 6.3.9600", "Windows 8.1", "Windows")]
    [InlineData("Microsoft Windows 7 Ultimate 6.1.7601", "Windows 7", "Windows")]
    [InlineData("Darwin 24.1.0 macOS Sequoia", "macOS", "macOS")]
    [InlineData("Linux 6.8.0-45-generic #45-Ubuntu SMP", "Ubuntu", "Linux")]
    [InlineData("Linux 6.1.0-26-amd64 Debian 12", "Debian", "Linux")]
    [InlineData("Linux 6.10.12-200.fc40.x86_64 Fedora", "Fedora", "Linux")]
    public void ClassifyOsDescription_AccuratelyMapsAllEightSupportedOsTargets(
        string rawOs,
        string expectedOsName,
        string expectedFamily)
    {
        var service = new ScriptKeyManagerService();
        var detected = service.ClassifyOsDescription(rawOs);

        Assert.Equal(expectedOsName, detected.OsName);
        Assert.Equal(expectedFamily, detected.OsFamily);
    }

    [Fact]
    public async Task GenerateScriptAndKeyAsync_ProducesValidOfKeyJsonAndLuaLoader()
    {
        var service = new ScriptKeyManagerService();
        var result = await service.GenerateScriptAndKeyAsync(
            overrideOsName: "Windows 11",
            overrideHwid: "HWID-UNITTEST-01",
            clientIp: "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal("OK", result.Code);
        Assert.NotNull(result.Payload);
        Assert.StartsWith("OF-", result.Payload.Key);
        Assert.Equal(12, result.Payload.Key.Length); // OF-XXXX-XXXX
        Assert.Equal("Windows 11", result.Payload.Os);
        Assert.Equal("1.0.0", result.Payload.Version);

        // Verify JSON structure matches 02_script_lua_mau.png
        var parsed = JsonSerializer.Deserialize<ScriptKeyPayloadDto>(result.RawJsonPayload);
        Assert.NotNull(parsed);
        Assert.Equal(result.Payload.Key, parsed.Key);
        Assert.Equal("Windows 11", parsed.Os);

        // Verify exact Lua loader line
        Assert.Contains(
            "loadstring(game:HttpGet(\"https://raw.githubusercontent.com/hyuttgg/kaitun/refs/heads/main/OceanForge%20desktop\"))()",
            result.CombinedClipboardScript);

        // Verify Encrypted Lua Source ("Mã hóa nguồn")
        Assert.Contains("OCEANFORGE PROTECTED LUA LOADER", result.EncryptedLuaScript);
        Assert.Contains("bit32.bxor", result.EncryptedLuaScript);
        Assert.Contains("loadstring(table.concat(_s))()", result.EncryptedLuaScript);
    }

    [Fact]
    public async Task GenerateScriptAndKeyAsync_EnforcesFiveMinuteDeviceCooldownAndThirtyMinuteSpamLockout()
    {
        var service = new ScriptKeyManagerService();

        // 1st request succeeds
        var first = await service.GenerateScriptAndKeyAsync(
            overrideOsName: "Windows 11",
            overrideHwid: "HWID-SPAM-TEST",
            clientIp: "192.168.1.50");
        Assert.True(first.Success);

        // 2nd request within 5 minutes is blocked with RATE_LIMIT (Strike 1)
        var second = await service.GenerateScriptAndKeyAsync(
            overrideOsName: "Windows 11",
            overrideHwid: "HWID-SPAM-TEST",
            clientIp: "192.168.1.50");
        Assert.False(second.Success);
        Assert.Equal("RATE_LIMIT", second.Code);
        Assert.Contains("Bạn đã tạo key quá nhiều lần. Vui lòng thử lại sau 5 phút.", second.Message);
        Assert.True(second.RetryAfterSeconds > 0 && second.RetryAfterSeconds <= 300);

        // 3rd & 4th rapid spam requests escalate to 30-minute temporary lock (1800s)
        await service.GenerateScriptAndKeyAsync("Windows 11", "HWID-SPAM-TEST", "192.168.1.50");
        var locked = await service.GenerateScriptAndKeyAsync("Windows 11", "HWID-SPAM-TEST", "192.168.1.50");

        Assert.False(locked.Success);
        Assert.Equal("RATE_LIMIT", locked.Code);
        Assert.Equal(1800, locked.RetryAfterSeconds);

        var status = service.GetAntiSpamStatus("HWID-SPAM-TEST", "Windows 11", "192.168.1.50");
        Assert.True(status.IsTemporarilyLocked);
    }

    [Fact]
    public async Task RevokeKeyAsync_MarksStatusRevokedEvictsCacheAndReturnsCanonicalJson()
    {
        var service = new ScriptKeyManagerService();
        var generated = await service.GenerateScriptAndKeyAsync(
            overrideOsName: "Ubuntu",
            overrideHwid: "HWID-REVOKE-99",
            clientIp: "10.0.0.9");

        Assert.True(generated.Success);
        string key = generated.Payload!.Key;

        var revokeResult = await service.RevokeKeyAsync(key, "Unit test manual revocation");

        Assert.True(revokeResult.Success);
        Assert.Equal("Key revoked", revokeResult.Message);
        Assert.Contains("\"success\": true", revokeResult.RawResponseJson);
        Assert.Contains("\"message\": \"Key revoked\"", revokeResult.RawResponseJson);

        var storedRecord = Assert.Single(service.GetAllKeys());
        Assert.Equal("revoked", storedRecord.Status);
        Assert.NotNull(storedRecord.RevokedAtUtc);
    }
}
