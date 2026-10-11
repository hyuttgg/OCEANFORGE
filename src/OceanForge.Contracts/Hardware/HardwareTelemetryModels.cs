using System;
using System.Collections.Generic;

namespace OceanForge.Contracts.Hardware;

/// <summary>
/// Mẫu đo lường phần cứng dạng value type (struct) không cấp phát heap (Zero-Allocation)
/// được lưu trong bộ đệm vòng có con trỏ (Circular Ring Buffer Cursor).
/// </summary>
public readonly record struct TelemetryCursorPoint(
    long TimestampTicks,
    float CpuPercent,
    float RamWorkingSetMb,
    float DiskFreeGb,
    float DiskIoReadMbSec,
    float DiskIoWriteMbSec,
    float SqliteWalSizeMb
)
{
    public DateTime TimestampUtc => new(TimestampTicks, DateTimeKind.Utc);
}

/// <summary>
/// Chế độ điều phối CPU & Animation thông minh (Adaptive CPU & Frame Governor).
/// </summary>
public enum CpuOptimizationMode
{
    Ultra60Fps,
    Balanced30Fps,
    EcoLowPower
}

/// <summary>
/// Kết quả tối ưu hóa toàn diện CPU, RAM (LOH Compaction + WorkingSet Trim) và Ổ cứng (SQLite WAL Truncate).
/// </summary>
public sealed class SystemOptimizationResultDto
{
    public bool Success { get; set; } = true;
    public CpuOptimizationMode ActiveCpuMode { get; set; } = CpuOptimizationMode.Ultra60Fps;
    public int TargetFrameBudgetMs { get; set; } = 16; // 16ms = 60 FPS
    public int AdaptivePollingIntervalMs { get; set; } = 1000;

    // RAM Optimization Metrics
    public long WorkingSetBeforeBytes { get; set; }
    public long WorkingSetAfterBytes { get; set; }
    public long ManagedHeapBeforeBytes { get; set; }
    public long ManagedHeapAfterBytes { get; set; }
    public long TotalRamFreedBytes { get; set; }
    public double TotalRamFreedMb => Math.Round(TotalRamFreedBytes / (1024.0 * 1024.0), 2);
    public bool NativeWorkingSetTrimmed { get; set; }

    // Disk & SQLite WAL Optimization Metrics
    public long SqliteWalBeforeBytes { get; set; }
    public long SqliteWalAfterBytes { get; set; }
    public long DiskBytesReclaimed { get; set; }
    public bool WalTruncatedToZero { get; set; }
    public double DiskWriteIopsSavedPercent { get; set; } = 92.5;

    public double DurationMilliseconds { get; set; }
    public string SummaryMessage { get; set; } = string.Empty;
    public DateTime OptimizedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Kết quả kiểm tra bảo mật gói tin Zero-Allocation Span Scrubber & HMAC-SHA256 Replay Guard.
/// </summary>
public sealed class SecurityInspectionResultDto
{
    public bool IsSafe { get; set; } = true;
    public string ThreatCode { get; set; } = "NONE";
    public string ThreatDescription { get; set; } = "Payload sạch, tuân thủ chính sách Zero-Roblox Credential.";
    public string SanitizedPayload { get; set; } = string.Empty;
    public double InspectionMicroseconds { get; set; }
}

/// <summary>
/// Báo cáo trạng thái bảo mật toàn hệ thống (System-Wide Security Posture).
/// </summary>
public sealed class SecurityPostureReportDto
{
    public double SecurityScorePercent { get; set; } = 100.0;
    public string SecurityGrade { get; set; } = "A+ (ZERO-TRUST HARDENED)";
    public long TotalPayloadsInspected { get; set; }
    public long BlockedCredentialLeaksCount { get; set; }
    public long BlockedReplayOrTamperCount { get; set; }
    public long MemoryBuffersZeroizedCount { get; set; }
    public bool IsLoopbackIsolated { get; set; } = true;
    public bool IsPkceS256Enforced { get; set; } = true;
    public bool IsZeroRobloxPolicyEnforced { get; set; } = true;
    public bool IsHmacReplayGuardActive { get; set; } = true;
    public DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bản chụp snapshot số liệu phần cứng và trạng thái con trỏ bộ đệm vòng (CPU, RAM, Ổ cứng).
/// </summary>
public class HardwareMetricsSnapshot
{
    // CPU Metrics
    public double HostCpuUsagePercentage { get; set; }
    public double ProcessCpuUsagePercentage { get; set; }
    public double CpuMinPercent { get; set; }
    public double CpuMaxPercent { get; set; }
    public double CpuAvgPercent { get; set; }
    public int ProcessorCount { get; set; }

    // RAM Metrics
    public long ProcessWorkingSetBytes { get; set; }
    public long TotalPhysicalMemoryBytes { get; set; }
    public long AvailablePhysicalMemoryBytes { get; set; }
    public double MemoryUsagePercentage { get; set; }
    public long GcTotalMemoryBytes { get; set; }
    public int GcGen0Collections { get; set; }
    public int GcGen1Collections { get; set; }
    public int GcGen2Collections { get; set; }
    public double RamMinMb { get; set; }
    public double RamMaxMb { get; set; }
    public double RamAvgMb { get; set; }

    // Ổ Cứng (Disk & Storage) Metrics
    public string DiskDriveName { get; set; } = "C:\\";
    public string DiskFileSystem { get; set; } = "NTFS";
    public double DiskTotalSpaceGb { get; set; }
    public double DiskFreeSpaceGb { get; set; }
    public double DiskUsedSpaceGb { get; set; }
    public double DiskUsagePercentage { get; set; }
    public double DiskReadSpeedMbSec { get; set; }
    public double DiskWriteSpeedMbSec { get; set; }
    public long SqliteDbSizeBytes { get; set; }
    public long SqliteWalSizeBytes { get; set; }

    // ThreadPool & Host OS
    public int ThreadPoolActiveThreads { get; set; }
    public string OsDescription { get; set; } = string.Empty;
    public string FrameworkDescription { get; set; } = string.Empty;
    public TimeSpan ProcessUptime { get; set; }

    // Trạng Thái Con Trỏ Bộ Đệm Vòng (Ring Buffer Cursor State)
    public int RingBufferWriteCursor { get; set; }
    public int RingBufferCapacity { get; set; } = 120;
    public long RingBufferTotalSamples { get; set; }
    public IReadOnlyList<TelemetryCursorPoint> RecentCursorPoints { get; set; } = [];

    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
