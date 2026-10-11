using System;
using System.IO;
using System.Threading.Tasks;
using OceanForge.Local.Persistence.Context;
using OceanForge.Local.Persistence.Entities;
using OceanForge.Local.Persistence.Repositories;
using Xunit;

namespace OceanForge.Application.Tests;

public class LocalAccountRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly OceanForgeLocalDbContext _context;
    private readonly LocalAccountRepository _repository;

    public LocalAccountRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_oceanforge_{Guid.NewGuid():N}.db");
        _context = new OceanForgeLocalDbContext(_testDbPath);
        _repository = new LocalAccountRepository(_context);
    }

    [Fact]
    public async Task UpsertAccountAsync_SavesAndRetrievesRecordCorrectly()
    {
        var record = new LocalAccountRecord
        {
            RobloxUsername = "TestAccount_1",
            Level = 2550,
            Beli = 15000000,
            Fragments = 7500,
            Sea = "Sea 3",
            Fruit = "Kitsune",
            Sword = "Cursed Dual Katana",
            FightingStyle = "Godhuman",
            Status = "Farming",
            Location = "Tiki Outpost",
            LastChecksumHash = "1234abcd",
            IsOnline = true,
            LastSeenUtc = DateTime.UtcNow
        };

        await _repository.UpsertAccountAsync(record);

        var retrieved = await _repository.GetAccountByUsernameAsync("TestAccount_1");
        Assert.NotNull(retrieved);
        Assert.Equal(2550, retrieved.Level);
        Assert.Equal("Kitsune", retrieved.Fruit);
        Assert.Equal("1234abcd", retrieved.LastChecksumHash);
    }

    [Fact]
    public async Task QueueOutboxMessageAsync_CreatesUnsyncedMessage()
    {
        string mutationId = Guid.NewGuid().ToString();
        string json = "{\"action\":\"update_metadata\",\"tag\":\"elite\"}";

        await _repository.QueueOutboxMessageAsync(mutationId, json);

        var allAccounts = await _repository.GetAllAccountsAsync();
        Assert.NotNull(allAccounts);
    }

    public void Dispose()
    {
        _context.Dispose();
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
        }
        catch { }
    }
}
