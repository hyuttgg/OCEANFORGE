using System;

namespace OceanForge.Domain.Entities;

public class TrackedAccount
{
    public Guid Id { get; private set; }
    public string RobloxUsername { get; private set; }
    public long Level { get; private set; }
    public long Beli { get; private set; }
    public long Fragments { get; private set; }
    public string Race { get; private set; }
    public string Sea { get; private set; }
    public string Fruit { get; private set; }
    public string Sword { get; private set; }
    public string FightingStyle { get; private set; }
    public string Status { get; private set; }
    public string Location { get; private set; }
    public long PlaytimeSeconds { get; private set; }
    public string DeviceHwid { get; private set; }
    public string Executor { get; private set; }
    public string LastChecksumHash { get; private set; }
    public int MagnetStage { get; private set; }
    public int SecretsCompleted { get; private set; }
    public long MagnetTokens { get; private set; }
    public DateTime LastSeenUtc { get; private set; }
    public bool IsOnline { get; private set; }

    public TrackedAccount(
        Guid id,
        string robloxUsername,
        long level,
        long beli,
        long fragments,
        string race,
        string sea,
        string fruit,
        string sword,
        string fightingStyle,
        string status,
        string location,
        long playtimeSeconds,
        string deviceHwid,
        string executor,
        string lastChecksumHash,
        int magnetStage,
        int secretsCompleted,
        long magnetTokens)
    {
        if (string.IsNullOrWhiteSpace(robloxUsername))
            throw new ArgumentException("Roblox username cannot be empty", nameof(robloxUsername));

        Id = id;
        RobloxUsername = robloxUsername;
        Level = level;
        Beli = beli;
        Fragments = fragments;
        Race = race;
        Sea = sea;
        Fruit = fruit;
        Sword = sword;
        FightingStyle = fightingStyle;
        Status = status;
        Location = location;
        PlaytimeSeconds = playtimeSeconds;
        DeviceHwid = deviceHwid;
        Executor = executor;
        LastChecksumHash = lastChecksumHash;
        MagnetStage = magnetStage;
        SecretsCompleted = secretsCompleted;
        MagnetTokens = magnetTokens;
        LastSeenUtc = DateTime.UtcNow;
        IsOnline = true;
    }

    public void UpdateTelemetry(
        long level,
        long beli,
        long fragments,
        string status,
        string location,
        string fruit,
        string sword,
        string fightingStyle,
        string checksumHash,
        int magnetStage,
        int secretsCompleted,
        long magnetTokens)
    {
        Level = level;
        Beli = beli;
        Fragments = fragments;
        Status = status;
        Location = location;
        Fruit = fruit;
        Sword = sword;
        FightingStyle = fightingStyle;
        LastChecksumHash = checksumHash;
        MagnetStage = magnetStage;
        SecretsCompleted = secretsCompleted;
        MagnetTokens = magnetTokens;
        LastSeenUtc = DateTime.UtcNow;
        IsOnline = true;
    }

    public void MarkOfflineIfStale(TimeSpan timeout)
    {
        if (DateTime.UtcNow - LastSeenUtc > timeout)
        {
            IsOnline = false;
        }
    }
}
