using System;
using System.Text;

namespace OceanForge.Domain.Hashing;

/// <summary>
/// FNV-1a 32-bit hashing implementation matching the Roblox executor script (sender copy.lua).
/// Used for high-speed deduplication across fleet telemetry.
/// </summary>
public static class Fnv1aHasher
{
    public const uint FnvOffsetBasis = 2166136261u;
    public const uint FnvPrime = 16777619u;

    public static string ComputeHexHash(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "00000000";
        }

        uint hash = FnvOffsetBasis;
        byte[] bytes = Encoding.UTF8.GetBytes(input);

        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return hash.ToString("x8");
    }

    /// <summary>
    /// Computes the exact FNV-1a hash formatted like sender copy.lua line 1738:
    /// username|level|beli|fragments|status|location|fruit|sword|magnetStage|secretsCompleted|magnetTokens
    /// </summary>
    public static string ComputeStateChecksum(
        string username,
        long level,
        long beli,
        long fragments,
        string status,
        string location,
        string fruit,
        string sword,
        int magnetStage,
        int secretsCompleted,
        long magnetTokens)
    {
        string raw = $"{username}|{level}|{beli}|{fragments}|{status}|{location}|{fruit}|{sword}|{magnetStage}|{secretsCompleted}|{magnetTokens}";
        return ComputeHexHash(raw);
    }
}
