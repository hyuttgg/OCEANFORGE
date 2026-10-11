using System;

namespace OceanForge.Contracts.Telemetry;

public class TelemetryIngestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string DedupHash { get; set; } = string.Empty;
    public bool IsStateUnchanged { get; set; }
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
