using System;
using System.Threading.Tasks;
using OceanForge.Application.Security;
using OceanForge.Contracts.Hardware;
using OceanForge.Telemetry.Services;
using Xunit;

namespace OceanForge.Application.Tests;

public class SystemOptimizerAndSecurityTests
{
    [Fact]
    public void InspectPayloadZeroAlloc_AllowsCleanPayload_AndBlocksForbiddenCredentials()
    {
        var security = new SystemSecurityHardeningService();

        var clean = security.InspectPayloadZeroAlloc("{\"username\":\"CaptainKha_77\",\"level\":2600,\"sea\":\"Sea 3\"}");
        Assert.True(clean.IsSafe);
        Assert.Equal("NONE", clean.ThreatCode);

        var robloCookieLeak = security.InspectPayloadZeroAlloc("{\"cookie\":\".ROBLOSECURITY=_|WARNING:-DO-NOT-SHARE-THIS\"}");
        Assert.False(robloCookieLeak.IsSafe);
        Assert.Equal("FORBIDDEN_SECRET_DETECTED", robloCookieLeak.ThreatCode);
        Assert.Equal("[REDACTED_BY_OCEANFORGE_SECURITY_GUARD]", robloCookieLeak.SanitizedPayload);

        var sqlInjectionAttempt = security.InspectPayloadZeroAlloc("{\"username\":\"admin'; DROP TABLE users; --\"}");
        Assert.False(sqlInjectionAttempt.IsSafe);
        Assert.Equal("FORBIDDEN_SECRET_DETECTED", sqlInjectionAttempt.ThreatCode);

        var report = security.GetSecurityPostureReport();
        Assert.Equal(100.0, report.SecurityScorePercent);
        Assert.Equal(3, report.TotalPayloadsInspected);
        Assert.Equal(2, report.BlockedCredentialLeaksCount);
    }

    [Fact]
    public void VerifyPayloadHmacAndReplay_AcceptsValidSignature_AndBlocksReplayOrExpiredTimestamp()
    {
        var security = new SystemSecurityHardeningService();
        long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string payload = "{\"username\":\"AbyssalRider_VN\",\"beli\":88200000}";
        string nonce = "nonce-unique-1001";

        string validSignature = security.SignPayloadHmac(payload, nonce, nowUnix);

        // 1st verification with valid nonce & signature succeeds
        bool firstCheck = security.VerifyPayloadHmacAndReplay(payload, nonce, nowUnix, validSignature);
        Assert.True(firstCheck);

        // 2nd verification with same nonce is blocked as a Replay Attack
        bool replayCheck = security.VerifyPayloadHmacAndReplay(payload, nonce, nowUnix, validSignature);
        Assert.False(replayCheck);

        // Expired timestamp (> 60s drift) is blocked
        long staleTimestamp = nowUnix - 120;
        string staleSig = security.SignPayloadHmac(payload, "nonce-stale-1002", staleTimestamp);
        bool expiredCheck = security.VerifyPayloadHmacAndReplay(payload, "nonce-stale-1002", staleTimestamp, staleSig);
        Assert.False(expiredCheck);
    }

    [Fact]
    public void ZeroizeSensitiveMemory_WipesBufferToAllZeros()
    {
        var security = new SystemSecurityHardeningService();
        byte[] secretBytes = [0x41, 0x42, 0x43, 0x44, 0xFF];

        security.ZeroizeSensitiveMemory(secretBytes);

        Assert.All(secretBytes, b => Assert.Equal(0, b));
        Assert.True(security.GetSecurityPostureReport().MemoryBuffersZeroizedCount >= 1);
    }

    [Theory]
    [InlineData(5.0, CpuOptimizationMode.Ultra60Fps, 16, 1000)]
    [InlineData(42.0, CpuOptimizationMode.Balanced30Fps, 33, 2000)]
    [InlineData(78.5, CpuOptimizationMode.EcoLowPower, 66, 5000)]
    public void EvaluateAdaptiveCpuBudget_SelectsOptimalFrameAndPollingCadence(
        double cpuLoad,
        CpuOptimizationMode expectedMode,
        int expectedFrameMs,
        int expectedPollMs)
    {
        var optimizer = new SystemResourceOptimizerService();
        var (mode, frameBudgetMs, pollingIntervalMs) = optimizer.EvaluateAdaptiveCpuBudget(cpuLoad);

        Assert.Equal(expectedMode, mode);
        Assert.Equal(expectedFrameMs, frameBudgetMs);
        Assert.Equal(expectedPollMs, pollingIntervalMs);
    }

    [Fact]
    public async Task OptimizeEntireSystemAsync_ExecutesCpuRamAndWalOptimizationPipeline()
    {
        var optimizer = new SystemResourceOptimizerService();

        var result = await optimizer.OptimizeEntireSystemAsync(
            currentCpuPercent: 12.4,
            sqliteWalOptimizer: _ => Task.FromResult((WalBefore: 262144L, WalAfter: 0L, Reclaimed: 262144L)));

        Assert.True(result.Success);
        Assert.Equal(CpuOptimizationMode.Ultra60Fps, result.ActiveCpuMode);
        Assert.Equal(16, result.TargetFrameBudgetMs);
        Assert.Equal(262144L, result.SqliteWalBeforeBytes);
        Assert.Equal(0L, result.SqliteWalAfterBytes);
        Assert.True(result.WalTruncatedToZero);
        Assert.Contains("Đã tối ưu toàn diện", result.SummaryMessage);
    }
}
