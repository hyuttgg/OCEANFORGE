using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using OceanForge.Local.Persistence.Entities;

namespace OceanForge.Local.Persistence.Context;

public class OceanForgeLocalDbContext : DbContext
{
    public DbSet<LocalAccountRecord> TrackedAccounts => Set<LocalAccountRecord>();
    public DbSet<TelemetryOutboxMessage> TelemetryOutbox => Set<TelemetryOutboxMessage>();
    public DbSet<SyncCursorRecord> SyncCursors => Set<SyncCursorRecord>();

    private readonly string _dbPath;
    public string DbPath => _dbPath;

    public OceanForgeLocalDbContext(DbContextOptions<OceanForgeLocalDbContext> options)
        : base(options)
    {
        _dbPath = "oceanforge_local.db";
    }

    public OceanForgeLocalDbContext(string? customDbPath = null)
    {
        if (string.IsNullOrWhiteSpace(customDbPath))
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appData, "OceanForge");
            Directory.CreateDirectory(folder);
            _dbPath = Path.Combine(folder, "oceanforge_local.db");
        }
        else
        {
            _dbPath = customDbPath;
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalAccountRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.RobloxUsername).IsUnique();
            b.Property(x => x.RobloxUsername).HasMaxLength(100).IsRequired();
            b.Property(x => x.LastChecksumHash).HasMaxLength(32);
        });

        modelBuilder.Entity<TelemetryOutboxMessage>(b =>
        {
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.IsSynced);
            b.Property(x => x.MutationId).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<SyncCursorRecord>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(100);
        });
    }

    public void InitializeDatabase()
    {
        Database.EnsureCreated();
    }
}
