using System.Threading.Tasks;
using OceanForge.AI.Services;
using Xunit;

namespace OceanForge.Application.Tests;

public class AiDebateServiceTests
{
    [Fact]
    public async Task RunDeliberationAsync_ProducesAllFourSpecializedAgents()
    {
        var service = new AiDebateService();
        var result = await service.RunDeliberationAsync("Kiểm định tải Ingestion Telemetry Roblox và Giám sát Phần cứng");

        Assert.NotNull(result);
        Assert.Equal(4, result.Debates.Count);

        // Verify all 4 specialized personas are represented
        Assert.Contains(result.Debates, d => d.AgentName.Contains("Architect"));
        Assert.Contains(result.Debates, d => d.AgentName.Contains("AppSec"));
        Assert.Contains(result.Debates, d => d.AgentName.Contains("Avalonia"));
        Assert.Contains(result.Debates, d => d.AgentName.Contains("Hardware"));

        Assert.False(string.IsNullOrWhiteSpace(result.ConsensusVerdict));
    }
}
