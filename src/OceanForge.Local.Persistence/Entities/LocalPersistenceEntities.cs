using System;

namespace OceanForge.Local.Persistence.Entities;

public class LocalAccountRecord
{
    public Guid Id { get; set; }
    public string RobloxUsername { get; set; } = string.Empty;
    public long Level { get; set; }
    public long Beli { get; set; }
    public long Fragments { get; set; }
    public string Race { get; set; } = string.Empty;
    public string Sea { get; set; } = string.Empty;
    public string Fruit { get; set; } = string.Empty;
    public string Sword { get; set; } = string.Empty;
    public string FightingStyle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public long PlaytimeSeconds { get; set; }
    public string Hwid { get; set; } = string.Empty;
    public string Executor { get; set; } = string.Empty;
    public string LastChecksumHash { get; set; } = string.Empty;
    public int MagnetStage { get; set; }
    public int SecretsCompleted { get; set; }
    public long MagnetTokens { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public bool IsOnline { get; set; }
}

public class TelemetryOutboxMessage
{
    public Guid Id { get; set; }
    public string MutationId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public bool IsSynced { get; set; }
    public DateTime? SyncedAtUtc { get; set; }
    public int RetryCount { get; set; }
}

public class SyncCursorRecord
{
    public string Id { get; set; } = string.Empty;
    public string CursorValue { get; set; } = string.Empty;
    public DateTime LastSyncTimeUtc { get; set; }
}
