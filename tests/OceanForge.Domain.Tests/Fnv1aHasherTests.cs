using OceanForge.Domain.Hashing;
using Xunit;

namespace OceanForge.Domain.Tests;

public class Fnv1aHasherTests
{
    [Fact]
    public void ComputeHexHash_WithNullOrEmpty_ReturnsZeros()
    {
        Assert.Equal("00000000", Fnv1aHasher.ComputeHexHash(null));
        Assert.Equal("00000000", Fnv1aHasher.ComputeHexHash(string.Empty));
    }

    [Fact]
    public void ComputeHexHash_WithKnownString_ReturnsConsistent8CharHex()
    {
        string input = "CaptainKha|2600|145000000|75200|Farming|Tiki Outpost|Kitsune|CDK|3|8|1250";
        string hash1 = Fnv1aHasher.ComputeHexHash(input);
        string hash2 = Fnv1aHasher.ComputeHexHash(input);

        Assert.NotNull(hash1);
        Assert.Equal(8, hash1.Length);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeStateChecksum_MatchesPayloadAttributesPattern()
    {
        string checksum = Fnv1aHasher.ComputeStateChecksum(
            username: "Player_1",
            level: 2550,
            beli: 50000000,
            fragments: 25000,
            status: "Raiding",
            location: "Cafe",
            fruit: "Dragon",
            sword: "Shark Anchor",
            magnetStage: 2,
            secretsCompleted: 5,
            magnetTokens: 800);

        Assert.Equal(8, checksum.Length);
    }
}
