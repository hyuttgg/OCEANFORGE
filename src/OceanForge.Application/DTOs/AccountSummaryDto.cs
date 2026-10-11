using System;

namespace OceanForge.Application.DTOs;

public class AccountSummaryDto
{
    public Guid Id { get; set; }
    public string RobloxUsername { get; set; } = string.Empty;
    public long Level { get; set; }
    public long Beli { get; set; }
    public long Fragments { get; set; }
    public string Race { get; set; } = "Human";
    public string Sea { get; set; } = "Sea 1";
    public string Fruit { get; set; } = "None";
    public string Sword { get; set; } = "None";
    public string Gun { get; set; } = "Soul Guitar";
    public string FightingStyle { get; set; } = "Combat";
    public string Status { get; set; } = "Idle";
    public string Location { get; set; } = "Spawn";
    public string ChecksumHash { get; set; } = string.Empty;
    public bool IsOnline { get; set; } = true;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    // Blox Fruits Update 30 fields
    public int MagnetStage { get; set; } = 1;
    public long MagnetPower { get; set; } = 0;
    public long TargetMagnetPower { get; set; } = 50000;
    public int SecretsCompleted { get; set; } = 0;
    public int TotalSecrets { get; set; } = 10;
    public double SecretsPercentage { get; set; } = 0.0;
    public long MagnetTokens { get; set; } = 0;

    // Device telemetry
    public string DeviceHwid { get; set; } = string.Empty;
    public string Executor { get; set; } = "Unknown";
    public double PlaytimeHours { get; set; } = 0.0;
}
