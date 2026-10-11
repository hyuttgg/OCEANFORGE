using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OceanForge.AI.Services;
using OceanForge.Contracts.Telemetry;
using OceanForge.Telemetry.HttpBridge;
using OceanForge.Telemetry.Services;
using Xunit;

namespace OceanForge.Application.Tests;

public class EndToEndTelemetryAndDebateTests
{
    [Fact]
    public async Task EndToEnd_TelemetryHttpBridge_IngestsAndDeduplicatesSuccessfully()
    {
        int testPort = 5099;
        await using var ingestionService = new TelemetryIngestionService();
        await using var bridgeServer = new LocalTelemetryBridgeServer(ingestionService, testPort);
        bridgeServer.Start();

        Assert.True(bridgeServer.IsRunning, "Bridge server should be running on test port 5099");

        using var client = new HttpClient();
        string baseUrl = $"http://127.0.0.1:{testPort}";

        // 1. Test Health Endpoint
        var healthResp = await client.GetAsync($"{baseUrl}/health");
        Assert.True(healthResp.IsSuccessStatusCode);
        string healthJson = await healthResp.Content.ReadAsStringAsync();
        Assert.Contains("OceanForge C# Engine", healthJson);

        // 2. Test Ingestion Endpoint (/api/lua/update) with Blox Fruits Update 30 payload
        var payload = new RobloxTelemetryPayload
        {
            RobloxUsername = "CaptainKha_77",
            Level = 2600,
            Beli = 145000000,
            Fragments = 75200,
            Race = "Cyborg V4",
            Sea = "Sea 3",
            Fruit = "Kitsune (Mastery 600)",
            Sword = "Cursed Dual Katana",
            FightingStyle = "Godhuman",
            Status = "Leviathan Hunting 🐉",
            Location = "Tiki Outpost",
            Playtime = 142000,
            Hwid = "HWID-X889-WINDOWS",
            Executor = "Wave v3.2",
            CsharpAccelerated = true,
            MagnetTokens = 1250,
            MagnetPower = new MagnetPowerDto { Stage = 3, Power = 85000, TargetPower = 100000 },
            Secrets = new SecretsProgressDto { Completed = 8, Total = 10, Percentage = 80.0 }
        };

        string jsonPayload = JsonSerializer.Serialize(payload);
        var content1 = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        content1.Headers.Add("x-api-key", "OF-KEY-LOCAL-DEV");

        var postResp1 = await client.PostAsync($"{baseUrl}/api/lua/update", content1);
        Assert.True(postResp1.IsSuccessStatusCode);

        string resJson1 = await postResp1.Content.ReadAsStringAsync();
        var result1 = JsonSerializer.Deserialize<TelemetryIngestResult>(resJson1, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result1);
        Assert.True(result1.Success);
        Assert.False(result1.IsStateUnchanged);
        Assert.NotNull(result1.DedupHash);

        // 3. Test Deduplication Fast-Path with identical payload
        var content2 = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var postResp2 = await client.PostAsync($"{baseUrl}/api/lua/update", content2);
        Assert.True(postResp2.IsSuccessStatusCode);

        string resJson2 = await postResp2.Content.ReadAsStringAsync();
        var result2 = JsonSerializer.Deserialize<TelemetryIngestResult>(resJson2, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result2);
        Assert.True(result2.Success);
        Assert.True(result2.IsStateUnchanged, "Second identical packet must be detected as fast-path deduplicated");
        Assert.Equal(result1.DedupHash, result2.DedupHash);
    }

    [Theory]
    [InlineData("Tối ưu hóa Ingestion Telemetry & FNV-1a Checksum", "Ingestion")]
    [InlineData("Bảo mật AppSec: Chặn lộ lọt Cookie và Roblox Credentials", "Bảo mật")]
    [InlineData("Phân bổ Tài nguyên Phần cứng và Ổn định UI 60 FPS", "Phần cứng")]
    [InlineData("Chiến lược Blox Fruits Update 30: Săn Leviathan & Tộc V4", "Blox Fruits")]
    public async Task EndToEnd_AiDebateService_GeneratesTargetedPerspectivesAndConsensus(string topic, string expectedKeyword)
    {
        var debateService = new AiDebateService();
        var session = await debateService.RunDeliberationAsync(topic);

        Assert.NotNull(session);
        Assert.Equal(4, session.Debates.Count);

        // All 4 key personas participate
        Assert.Contains(session.Debates, d => d.AgentName.Contains("Architect"));
        Assert.Contains(session.Debates, d => d.AgentName.Contains("AppSec"));
        Assert.Contains(session.Debates, d => d.AgentName.Contains("Avalonia"));
        Assert.Contains(session.Debates, d => d.AgentName.Contains("Hardware"));

        // Consensus verdict is formed and contains domain keyword
        Assert.False(string.IsNullOrWhiteSpace(session.ConsensusVerdict));
        Assert.True(session.ConsensusVerdict.Length > 20);
        Assert.Contains(expectedKeyword, session.ConsensusVerdict, StringComparison.OrdinalIgnoreCase);
    }
}
