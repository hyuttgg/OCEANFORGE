using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OceanForge.Contracts.Telemetry;

/// <summary>
/// Telemetry payload emitted by the Roblox client executor script (sender copy.lua).
/// </summary>
public class RobloxTelemetryPayload
{
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("robloxUsername")]
    public string RobloxUsername { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("androidId")]
    public string? AndroidId { get; set; }

    [JsonPropertyName("hwid")]
    public string Hwid { get; set; } = string.Empty;

    [JsonPropertyName("sameHwid")]
    public bool? SameHwid { get; set; }

    [JsonPropertyName("activeHub")]
    public string? ActiveHub { get; set; }

    [JsonPropertyName("currentHub")]
    public string? CurrentHub { get; set; }

    [JsonPropertyName("level")]
    public long Level { get; set; }

    [JsonPropertyName("beli")]
    public long Beli { get; set; }

    [JsonPropertyName("fragments")]
    public long Fragments { get; set; }

    [JsonPropertyName("race")]
    public string Race { get; set; } = string.Empty;

    [JsonPropertyName("sea")]
    public string Sea { get; set; } = string.Empty;

    [JsonPropertyName("fruit")]
    public string Fruit { get; set; } = string.Empty;

    [JsonPropertyName("fruit_equipped")]
    public string? FruitEquipped { get; set; }

    [JsonPropertyName("fruit_mastery")]
    public long? FruitMastery { get; set; }

    [JsonPropertyName("sword")]
    public string Sword { get; set; } = string.Empty;

    [JsonPropertyName("gun")]
    public string Gun { get; set; } = string.Empty;

    [JsonPropertyName("fightingStyle")]
    public string FightingStyle { get; set; } = string.Empty;

    [JsonPropertyName("fighting_style")]
    public string? FightingStyleAlias { get; set; }

    [JsonPropertyName("accessory_equipped")]
    public string? AccessoryEquipped { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("playtime")]
    public long Playtime { get; set; }

    [JsonPropertyName("device")]
    public string Device { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("executor")]
    public string Executor { get; set; } = string.Empty;

    [JsonPropertyName("csharpAccelerated")]
    public bool CsharpAccelerated { get; set; }

    [JsonPropertyName("inventory")]
    public List<string>? Inventory { get; set; } = [];

    [JsonPropertyName("magnetPower")]
    public MagnetPowerDto? MagnetPower { get; set; }

    [JsonPropertyName("secrets")]
    public SecretsProgressDto? Secrets { get; set; }

    [JsonPropertyName("magnetTokens")]
    public long MagnetTokens { get; set; }

    [JsonPropertyName("gachaItems")]
    public List<string>? GachaItems { get; set; } = [];
}

public class MagnetPowerDto
{
    [JsonPropertyName("stage")]
    public int Stage { get; set; }

    [JsonPropertyName("power")]
    public long Power { get; set; }

    [JsonPropertyName("targetPower")]
    public long TargetPower { get; set; }
}

public class SecretsProgressDto
{
    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }
}
