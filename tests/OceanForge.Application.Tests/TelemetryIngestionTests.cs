using System.Threading.Tasks;
using OceanForge.Contracts.Telemetry;
using OceanForge.Telemetry.Services;
using Xunit;

namespace OceanForge.Application.Tests;

public class TelemetryIngestionTests
{
    [Fact]
    public async Task IngestAsync_WithValidRobloxPayload_ReturnsSuccessAndDedupHash()
    {
        await using var ingestionService = new TelemetryIngestionService();

        var payload = new RobloxTelemetryPayload
        {
            RobloxUsername = "TestPlayer_99",
            Level = 2550,
            Beli = 10000000,
            Fragments = 5000,
            Race = "Human",
            Sea = "Sea 3",
            Fruit = "Dough",
            Sword = "Yama",
            Status = "Farming",
            Location = "Floating Turtle",
            Playtime = 12000,
            Hwid = "TEST-HWID-123"
        };

        var result = await ingestionService.IngestAsync(payload);

        Assert.True(result.Success);
        Assert.False(result.IsStateUnchanged);
        Assert.NotNull(result.DedupHash);
        Assert.Equal(8, result.DedupHash.Length);
    }

    [Fact]
    public async Task IngestAsync_WithIdenticalPayload_ActivatesFastPathDeduplication()
    {
        await using var ingestionService = new TelemetryIngestionService();

        var payload = new RobloxTelemetryPayload
        {
            RobloxUsername = "DedupPlayer",
            Level = 2600,
            Beli = 50000000,
            Fragments = 20000,
            Race = "Cyborg",
            Sea = "Sea 3",
            Fruit = "Kitsune",
            Sword = "Cursed Dual Katana",
            Status = "Idle",
            Location = "Tiki Outpost"
        };

        // First ingestion
        var firstResult = await ingestionService.IngestAsync(payload);
        Assert.True(firstResult.Success);

        // Give background queue a moment to update cache
        await Task.Delay(100);

        // Second identical ingestion
        var secondResult = await ingestionService.IngestAsync(payload);
        Assert.True(secondResult.Success);
        Assert.True(secondResult.IsStateUnchanged);
        Assert.Equal(firstResult.DedupHash, secondResult.DedupHash);
    }

    [Fact]
    public async Task IngestAsync_MissingUsername_FailsGracefully()
    {
        await using var ingestionService = new TelemetryIngestionService();

        var payload = new RobloxTelemetryPayload
        {
            RobloxUsername = "",
            Username = "",
            Level = 100
        };

        var result = await ingestionService.IngestAsync(payload);

        Assert.False(result.Success);
        Assert.Contains("Missing username", result.Message);
    }
}
