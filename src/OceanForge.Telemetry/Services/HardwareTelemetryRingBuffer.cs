using System;
using System.Threading;
using OceanForge.Contracts.Hardware;

namespace OceanForge.Telemetry.Services;

/// <summary>
/// Thuật toán con trỏ bộ nhớ đệm vòng (Zero-Allocation Circular Ring Buffer with Head/Tail Pointer).
/// Quản lý chuỗi thời gian telemetry CPU, RAM, Ổ CỨNG với chi phí cấp phát heap = 0 byte khi vận hành liên tục.
/// </summary>
public class HardwareTelemetryRingBuffer
{
    private readonly TelemetryCursorPoint[] _buffer;
    private readonly Lock _bufferLock = new();
    private int _writeCursor;
    private int _count;
    private long _totalSamples;

    public int Capacity => _buffer.Length;
    public int Count { get { lock (_bufferLock) return _count; } }
    public int WriteCursor { get { lock (_bufferLock) return _writeCursor; } }
    public long TotalSamples { get { lock (_bufferLock) return _totalSamples; } }

    public HardwareTelemetryRingBuffer(int capacity = 120)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity phải lớn hơn 0");
        _buffer = new TelemetryCursorPoint[capacity];
        _writeCursor = 0;
        _count = 0;
        _totalSamples = 0;
    }

    /// <summary>
    /// Đẩy một mẫu telemetry mới vào vị trí con trỏ hiện tại và tăng con trỏ tuần hoàn (Modulo wrap-around).
    /// </summary>
    public void Push(in TelemetryCursorPoint point)
    {
        lock (_bufferLock)
        {
            _buffer[_writeCursor] = point;
            _writeCursor = (_writeCursor + 1) % _buffer.Length;
            if (_count < _buffer.Length)
            {
                _count++;
            }
            _totalSamples++;
        }
    }

    public void Push(float cpu, float ramMb, float diskFreeGb, float diskReadMbSec = 0f, float diskWriteMbSec = 0f, float sqliteWalMb = 0f)
    {
        var point = new TelemetryCursorPoint(
            DateTime.UtcNow.Ticks,
            cpu,
            ramMb,
            diskFreeGb,
            diskReadMbSec,
            diskWriteMbSec,
            sqliteWalMb
        );
        Push(in point);
    }

    /// <summary>
    /// Lấy mẫu telemetry mới nhất vừa được ghi.
    /// </summary>
    public TelemetryCursorPoint GetLatest()
    {
        lock (_bufferLock)
        {
            if (_count == 0) return default;
            int lastIndex = (_writeCursor - 1 + _buffer.Length) % _buffer.Length;
            return _buffer[lastIndex];
        }
    }

    /// <summary>
    /// Trích xuất toàn bộ mẫu theo thứ tự thời gian tuần tự (từ cũ nhất đến mới nhất) dựa trên vị trí con trỏ.
    /// </summary>
    public TelemetryCursorPoint[] ToChronologicalArray()
    {
        lock (_bufferLock)
        {
            if (_count == 0) return [];

            var result = new TelemetryCursorPoint[_count];
            if (_count < _buffer.Length)
            {
                // Chưa bị tràn vòng lặp: đọc tuần tự từ index 0 đến count - 1
                Array.Copy(_buffer, 0, result, 0, _count);
            }
            else
            {
                // Đã tràn vòng lặp: phần tử cũ nhất nằm ngay tại _writeCursor
                int tailLength = _buffer.Length - _writeCursor;
                Array.Copy(_buffer, _writeCursor, result, 0, tailLength);
                if (_writeCursor > 0)
                {
                    Array.Copy(_buffer, 0, result, tailLength, _writeCursor);
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Tính toán thống kê CPU (Min, Max, Avg) trực tiếp từ mảng con trỏ không cấp phát bộ nhớ.
    /// </summary>
    public void GetCpuStats(out double min, out double max, out double avg)
    {
        lock (_bufferLock)
        {
            if (_count == 0)
            {
                min = max = avg = 0;
                return;
            }

            double sum = 0;
            double localMin = double.MaxValue;
            double localMax = double.MinValue;

            for (int i = 0; i < _count; i++)
            {
                float val = _buffer[i].CpuPercent;
                sum += val;
                if (val < localMin) localMin = val;
                if (val > localMax) localMax = val;
            }

            min = Math.Round(localMin, 1);
            max = Math.Round(localMax, 1);
            avg = Math.Round(sum / _count, 1);
        }
    }

    /// <summary>
    /// Tính toán thống kê RAM (Min, Max, Avg) tính bằng MB trực tiếp từ con trỏ.
    /// </summary>
    public void GetRamStats(out double min, out double max, out double avg)
    {
        lock (_bufferLock)
        {
            if (_count == 0)
            {
                min = max = avg = 0;
                return;
            }

            double sum = 0;
            double localMin = double.MaxValue;
            double localMax = double.MinValue;

            for (int i = 0; i < _count; i++)
            {
                float val = _buffer[i].RamWorkingSetMb;
                sum += val;
                if (val < localMin) localMin = val;
                if (val > localMax) localMax = val;
            }

            min = Math.Round(localMin, 1);
            max = Math.Round(localMax, 1);
            avg = Math.Round(sum / _count, 1);
        }
    }

    /// <summary>
    /// Tính toán thống kê Dung lượng trống Ổ CỨNG (Min, Max, Avg) tính bằng GB.
    /// </summary>
    public void GetDiskStats(out double minFree, out double maxFree, out double avgFree)
    {
        lock (_bufferLock)
        {
            if (_count == 0)
            {
                minFree = maxFree = avgFree = 0;
                return;
            }

            double sum = 0;
            double localMin = double.MaxValue;
            double localMax = double.MinValue;

            for (int i = 0; i < _count; i++)
            {
                float val = _buffer[i].DiskFreeGb;
                sum += val;
                if (val < localMin) localMin = val;
                if (val > localMax) localMax = val;
            }

            minFree = Math.Round(localMin, 1);
            maxFree = Math.Round(localMax, 1);
            avgFree = Math.Round(sum / _count, 1);
        }
    }

    public void Clear()
    {
        lock (_bufferLock)
        {
            Array.Clear(_buffer);
            _writeCursor = 0;
            _count = 0;
        }
    }
}
