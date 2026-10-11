using System;
using System.Threading;
using OceanForge.Contracts.Hardware;
using OceanForge.Telemetry.Services;
using Xunit;

namespace OceanForge.Application.Tests;

public class HardwareMonitoringServiceTests
{
    [Fact]
    public void GetCurrentSnapshot_ReturnsValidSystemAndProcessMetrics()
    {
        var service = new HardwareMonitoringService();
        var snapshot = service.GetCurrentSnapshot();

        Assert.NotNull(snapshot);
        Assert.True(snapshot.ProcessorCount > 0);
        Assert.True(snapshot.ProcessWorkingSetBytes > 0);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.OsDescription));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.FrameworkDescription));
        Assert.True(snapshot.HostCpuUsagePercentage >= 0.0);
        Assert.True(snapshot.DiskTotalSpaceGb > 0);
        Assert.True(snapshot.ProcessUptime >= TimeSpan.Zero);
    }

    [Fact]
    public void HardwareMonitoringService_TracksDiskSubsystemAndWalSize()
    {
        var service = new HardwareMonitoringService();
        var snapshot = service.GetCurrentSnapshot();

        Assert.NotNull(snapshot.DiskDriveName);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.DiskFileSystem));
        Assert.True(snapshot.DiskTotalSpaceGb > 0);
        Assert.True(snapshot.DiskFreeSpaceGb >= 0);
        Assert.True(snapshot.DiskUsedSpaceGb >= 0);
        Assert.True(snapshot.DiskUsagePercentage >= 0.0 && snapshot.DiskUsagePercentage <= 100.0);
        Assert.True(snapshot.SqliteDbSizeBytes >= 0);
        Assert.True(snapshot.SqliteWalSizeBytes >= 0);
    }

    [Fact]
    public void RingBuffer_InitialState_CorrectPointersAndZeroCount()
    {
        var ringBuffer = new HardwareTelemetryRingBuffer(120);

        Assert.Equal(120, ringBuffer.Capacity);
        Assert.Equal(0, ringBuffer.Count);
        Assert.Equal(0, ringBuffer.WriteCursor);
        Assert.Equal(0, ringBuffer.TotalSamples);
        Assert.Empty(ringBuffer.ToChronologicalArray());
    }

    [Fact]
    public void RingBuffer_PushWithinCapacity_PointersIncrementSequentially()
    {
        var ringBuffer = new HardwareTelemetryRingBuffer(5);

        ringBuffer.Push(cpu: 10f, ramMb: 100f, diskFreeGb: 50f);
        ringBuffer.Push(cpu: 20f, ramMb: 110f, diskFreeGb: 49f);
        ringBuffer.Push(cpu: 30f, ramMb: 120f, diskFreeGb: 48f);

        Assert.Equal(3, ringBuffer.Count);
        Assert.Equal(3, ringBuffer.WriteCursor);
        Assert.Equal(3, ringBuffer.TotalSamples);

        var samples = ringBuffer.ToChronologicalArray();
        Assert.Equal(3, samples.Length);
        Assert.Equal(10f, samples[0].CpuPercent);
        Assert.Equal(20f, samples[1].CpuPercent);
        Assert.Equal(30f, samples[2].CpuPercent);
    }

    [Fact]
    public void RingBuffer_PushBeyondCapacity_WrapsAroundModuloCorrectly()
    {
        int capacity = 4;
        var ringBuffer = new HardwareTelemetryRingBuffer(capacity);

        // Push 4 items to fill buffer
        ringBuffer.Push(cpu: 10f, ramMb: 100f, diskFreeGb: 50f);
        ringBuffer.Push(cpu: 20f, ramMb: 110f, diskFreeGb: 49f);
        ringBuffer.Push(cpu: 30f, ramMb: 120f, diskFreeGb: 48f);
        ringBuffer.Push(cpu: 40f, ramMb: 130f, diskFreeGb: 47f);

        Assert.Equal(4, ringBuffer.Count);
        Assert.Equal(0, ringBuffer.WriteCursor); // Wrapped back to 0!
        Assert.Equal(4, ringBuffer.TotalSamples);

        // Push 2 more items to overwrite oldest items at slots 0 and 1
        ringBuffer.Push(cpu: 50f, ramMb: 140f, diskFreeGb: 46f);
        ringBuffer.Push(cpu: 60f, ramMb: 150f, diskFreeGb: 45f);

        Assert.Equal(4, ringBuffer.Count);
        Assert.Equal(2, ringBuffer.WriteCursor); // Now points to slot 2
        Assert.Equal(6, ringBuffer.TotalSamples);

        // Chronological order should be: 30 (slot 2), 40 (slot 3), 50 (slot 0), 60 (slot 1)
        var samples = ringBuffer.ToChronologicalArray();
        Assert.Equal(4, samples.Length);
        Assert.Equal(30f, samples[0].CpuPercent);
        Assert.Equal(40f, samples[1].CpuPercent);
        Assert.Equal(50f, samples[2].CpuPercent);
        Assert.Equal(60f, samples[3].CpuPercent);
    }

    [Fact]
    public void RingBuffer_GetCpuRamDiskStats_CalculatesMinMaxAvgAccurately()
    {
        var ringBuffer = new HardwareTelemetryRingBuffer(10);

        ringBuffer.Push(cpu: 10f, ramMb: 100f, diskFreeGb: 50f);
        ringBuffer.Push(cpu: 20f, ramMb: 200f, diskFreeGb: 40f);
        ringBuffer.Push(cpu: 30f, ramMb: 300f, diskFreeGb: 60f);

        ringBuffer.GetCpuStats(out double minCpu, out double maxCpu, out double avgCpu);
        Assert.Equal(10.0, minCpu);
        Assert.Equal(30.0, maxCpu);
        Assert.Equal(20.0, avgCpu);

        ringBuffer.GetRamStats(out double minRam, out double maxRam, out double avgRam);
        Assert.Equal(100.0, minRam);
        Assert.Equal(300.0, maxRam);
        Assert.Equal(200.0, avgRam);

        ringBuffer.GetDiskStats(out double minDisk, out double maxDisk, out double avgDisk);
        Assert.Equal(40.0, minDisk);
        Assert.Equal(60.0, maxDisk);
        Assert.Equal(50.0, avgDisk);
    }

    [Fact]
    public void RingBuffer_GetLatest_ReturnsMostRecentSample()
    {
        var ringBuffer = new HardwareTelemetryRingBuffer(5);

        ringBuffer.Push(cpu: 15f, ramMb: 80f, diskFreeGb: 100f);
        ringBuffer.Push(cpu: 25f, ramMb: 90f, diskFreeGb: 99f);

        var latest = ringBuffer.GetLatest();
        Assert.Equal(25f, latest.CpuPercent);
        Assert.Equal(90f, latest.RamWorkingSetMb);
        Assert.Equal(99f, latest.DiskFreeGb);
    }

    [Fact]
    public void HardwareMonitoringService_IntegratesRingBufferAcrossMultipleSnapshots()
    {
        var service = new HardwareMonitoringService(ringBufferCapacity: 10);

        var snap1 = service.GetCurrentSnapshot();
        Assert.Equal(1, snap1.RingBufferWriteCursor);
        Assert.Equal(1, snap1.RingBufferTotalSamples);
        Assert.Single(snap1.RecentCursorPoints);

        var snap2 = service.GetCurrentSnapshot();
        Assert.Equal(2, snap2.RingBufferWriteCursor);
        Assert.Equal(2, snap2.RingBufferTotalSamples);
        Assert.Equal(2, snap2.RecentCursorPoints.Count);
    }
}
