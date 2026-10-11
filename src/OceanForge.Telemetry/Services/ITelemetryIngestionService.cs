using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OceanForge.Contracts.Telemetry;
using OceanForge.Domain.Entities;

namespace OceanForge.Telemetry.Services;

public interface ITelemetryIngestionService
{
    Task<TelemetryIngestResult> IngestAsync(RobloxTelemetryPayload payload, CancellationToken cancellationToken = default);
    IReadOnlyCollection<TrackedAccount> GetActiveAccounts();
    TrackedAccount? GetAccount(string username);
    double CurrentPacketsPerSecond { get; }
    long TotalPacketsIngested { get; }
    long DeduplicatedPacketsCount { get; }
    event Action<TrackedAccount>? OnAccountUpdated;
}
