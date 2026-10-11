using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OceanForge.Local.Persistence.Context;
using OceanForge.Local.Persistence.Entities;

namespace OceanForge.Local.Persistence.Repositories;

public interface ILocalAccountRepository
{
    Task UpsertAccountAsync(LocalAccountRecord account, CancellationToken cancellationToken = default);
    Task<List<LocalAccountRecord>> GetAllAccountsAsync(CancellationToken cancellationToken = default);
    Task<LocalAccountRecord?> GetAccountByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task QueueOutboxMessageAsync(string mutationId, string payloadJson, CancellationToken cancellationToken = default);
    Task<(long WalBeforeBytes, long WalAfterBytes, long ReclaimedBytes)> OptimizeSqliteWalAndVacuumAsync(CancellationToken cancellationToken = default);
}

public class LocalAccountRepository : ILocalAccountRepository
{
    private readonly OceanForgeLocalDbContext _context;

    public LocalAccountRepository(OceanForgeLocalDbContext context)
    {
        _context = context;
        _context.InitializeDatabase();
    }

    public async Task UpsertAccountAsync(LocalAccountRecord account, CancellationToken cancellationToken = default)
    {
        var existing = await _context.TrackedAccounts
            .FirstOrDefaultAsync(x => x.RobloxUsername == account.RobloxUsername, cancellationToken);

        if (existing == null)
        {
            if (account.Id == Guid.Empty) account.Id = Guid.NewGuid();
            await _context.TrackedAccounts.AddAsync(account, cancellationToken);
        }
        else
        {
            existing.Level = account.Level;
            existing.Beli = account.Beli;
            existing.Fragments = account.Fragments;
            existing.Race = account.Race;
            existing.Sea = account.Sea;
            existing.Fruit = account.Fruit;
            existing.Sword = account.Sword;
            existing.FightingStyle = account.FightingStyle;
            existing.Status = account.Status;
            existing.Location = account.Location;
            existing.PlaytimeSeconds = account.PlaytimeSeconds;
            existing.LastChecksumHash = account.LastChecksumHash;
            existing.MagnetStage = account.MagnetStage;
            existing.SecretsCompleted = account.SecretsCompleted;
            existing.MagnetTokens = account.MagnetTokens;
            existing.LastSeenUtc = account.LastSeenUtc;
            existing.IsOnline = account.IsOnline;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<LocalAccountRecord>> GetAllAccountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TrackedAccounts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<LocalAccountRecord?> GetAccountByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.TrackedAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RobloxUsername == username, cancellationToken);
    }

    public async Task QueueOutboxMessageAsync(string mutationId, string payloadJson, CancellationToken cancellationToken = default)
    {
        var outbox = new TelemetryOutboxMessage
        {
            Id = Guid.NewGuid(),
            MutationId = mutationId,
            PayloadJson = payloadJson,
            CreatedAtUtc = DateTime.UtcNow,
            IsSynced = false,
            RetryCount = 0
        };
        await _context.TelemetryOutbox.AddAsync(outbox, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Thực thi thuật toán tối ưu ổ cứng SQLite WAL:
    /// 1. PRAGMA wal_checkpoint(TRUNCATE) để gộp toàn bộ trang ghi từ file -wal về DB chính và cắt file -wal về 0 byte.
    /// 2. PRAGMA optimize để tối ưu cây chỉ mục B-Tree giảm IOPS đọc/ghi đĩa.
    /// 3. PRAGMA shrink_memory để giải phóng bộ nhớ đệm trang của SQLite.
    /// </summary>
    public async Task<(long WalBeforeBytes, long WalAfterBytes, long ReclaimedBytes)> OptimizeSqliteWalAndVacuumAsync(CancellationToken cancellationToken = default)
    {
        string walPath = _context.DbPath + "-wal";
        long walBefore = File.Exists(walPath) ? new FileInfo(walPath).Length : 0;

        try
        {
            // Remove already synced outbox records older than 1 hour to keep disk footprint lean
            var cutoff = DateTime.UtcNow.AddHours(-1);
            await _context.TelemetryOutbox
                .Where(m => m.IsSynced && m.CreatedAtUtc < cutoff)
                .ExecuteDeleteAsync(cancellationToken);

            await _context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(TRUNCATE);", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("PRAGMA optimize;", cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("PRAGMA shrink_memory;", cancellationToken);
        }
        catch
        {
            // Fallback if InMemory or locked during concurrent test
        }

        long walAfter = File.Exists(walPath) ? new FileInfo(walPath).Length : 0;
        long reclaimed = Math.Max(0, walBefore - walAfter);
        return (walBefore, walAfter, reclaimed);
    }
}
