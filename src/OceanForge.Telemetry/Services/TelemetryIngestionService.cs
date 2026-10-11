using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using OceanForge.Contracts.Telemetry;
using OceanForge.Domain.Entities;
using OceanForge.Domain.Hashing;

namespace OceanForge.Telemetry.Services;

public class TelemetryIngestionService : ITelemetryIngestionService, IAsyncDisposable
{
    private readonly Channel<RobloxTelemetryPayload> _channel;
    private readonly ConcurrentDictionary<string, TrackedAccount> _accounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _latestChecksums = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _processingTask;

    private long _totalPackets;
    private long _dedupPackets;
    private long _recentPacketsWindow;
    private DateTime _lastRateCheck = DateTime.UtcNow;
    private double _currentRate;

    public double CurrentPacketsPerSecond => _currentRate;
    public long TotalPacketsIngested => Interlocked.Read(ref _totalPackets);
    public long DeduplicatedPacketsCount => Interlocked.Read(ref _dedupPackets);

    public event Action<TrackedAccount>? OnAccountUpdated;

    public TelemetryIngestionService(int bufferCapacity = 10000)
    {
        var options = new BoundedChannelOptions(bufferCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<RobloxTelemetryPayload>(options);
        _processingTask = Task.Run(ProcessQueueAsync);
    }

    public async Task<TelemetryIngestResult> IngestAsync(RobloxTelemetryPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));

        string username = !string.IsNullOrWhiteSpace(payload.RobloxUsername)
            ? payload.RobloxUsername
            : payload.Username;

        if (string.IsNullOrWhiteSpace(username))
        {
            return new TelemetryIngestResult
            {
                Success = false,
                Message = "Missing username in telemetry payload",
                DedupHash = "00000000",
                IsStateUnchanged = false
            };
        }

        int magnetStage = payload.MagnetPower?.Stage ?? 0;
        int secretsCompleted = payload.Secrets?.Completed ?? 0;
        long magnetTokens = payload.MagnetTokens;

        string computedHash = Fnv1aHasher.ComputeStateChecksum(
            username,
            payload.Level,
            payload.Beli,
            payload.Fragments,
            payload.Status,
            payload.Location,
            payload.Fruit,
            payload.Sword,
            magnetStage,
            secretsCompleted,
            magnetTokens);

        // Fast-path deduplication: check synchronous checksum cache first or account cache
        if (_latestChecksums.TryGetValue(username, out var lastHash) && lastHash == computedHash)
        {
            Interlocked.Increment(ref _totalPackets);
            Interlocked.Increment(ref _dedupPackets);
            UpdateRateCounter();

            return new TelemetryIngestResult
            {
                Success = true,
                Message = "State unchanged (Fast-path deduped)",
                DedupHash = computedHash,
                IsStateUnchanged = true
            };
        }

        // Store latest checksum synchronously before enqueueing to ensure race-free deduplication
        _latestChecksums[username] = computedHash;

        // Write to bounded channel
        await _channel.Writer.WriteAsync(payload, cancellationToken);
        Interlocked.Increment(ref _totalPackets);
        UpdateRateCounter();

        return new TelemetryIngestResult
        {
            Success = true,
            Message = "Telemetry ingested successfully",
            DedupHash = computedHash,
            IsStateUnchanged = false
        };
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _channel.Reader;
        while (!_cts.IsCancellationRequested && await reader.WaitToReadAsync(_cts.Token))
        {
            while (reader.TryRead(out var payload))
            {
                string username = !string.IsNullOrWhiteSpace(payload.RobloxUsername)
                    ? payload.RobloxUsername
                    : payload.Username;

                int magnetStage = payload.MagnetPower?.Stage ?? 0;
                int secretsCompleted = payload.Secrets?.Completed ?? 0;
                long magnetTokens = payload.MagnetTokens;

                string computedHash = Fnv1aHasher.ComputeStateChecksum(
                    username,
                    payload.Level,
                    payload.Beli,
                    payload.Fragments,
                    payload.Status,
                    payload.Location,
                    payload.Fruit,
                    payload.Sword,
                    magnetStage,
                    secretsCompleted,
                    magnetTokens);

                var account = _accounts.AddOrUpdate(
                    username,
                    key => new TrackedAccount(
                        Guid.NewGuid(),
                        key,
                        payload.Level,
                        payload.Beli,
                        payload.Fragments,
                        payload.Race,
                        payload.Sea,
                        payload.Fruit,
                        payload.Sword,
                        payload.FightingStyle,
                        payload.Status,
                        payload.Location,
                        payload.Playtime,
                        payload.Hwid,
                        payload.Executor,
                        computedHash,
                        magnetStage,
                        secretsCompleted,
                        magnetTokens),
                    (key, existing) =>
                    {
                        existing.UpdateTelemetry(
                            payload.Level,
                            payload.Beli,
                            payload.Fragments,
                            payload.Status,
                            payload.Location,
                            payload.Fruit,
                            payload.Sword,
                            payload.FightingStyle,
                            computedHash,
                            magnetStage,
                            secretsCompleted,
                            magnetTokens);
                        return existing;
                    });

                OnAccountUpdated?.Invoke(account);
            }
        }
    }

    private void UpdateRateCounter()
    {
        Interlocked.Increment(ref _recentPacketsWindow);
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRateCheck).TotalSeconds;
        if (elapsed >= 1.0)
        {
            long packets = Interlocked.Exchange(ref _recentPacketsWindow, 0);
            _currentRate = Math.Round(packets / elapsed, 1);
            _lastRateCheck = now;
        }
    }

    public IReadOnlyCollection<TrackedAccount> GetActiveAccounts()
    {
        return _accounts.Values.ToList();
    }

    public TrackedAccount? GetAccount(string username)
    {
        return _accounts.TryGetValue(username, out var acc) ? acc : null;
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        try
        {
            await _processingTask;
        }
        catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
