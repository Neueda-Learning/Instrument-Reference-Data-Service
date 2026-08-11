using Microsoft.EntityFrameworkCore;
using InstrumentReferenceDataService.Models;

namespace InstrumentReferenceDataService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AssetClass> AssetClasses => Set<AssetClass>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Exchange> Exchanges => Set<Exchange>();
    public DbSet<IdentifierType> IdentifierTypes => Set<IdentifierType>();
    public DbSet<Instrument> Instruments => Set<Instrument>();
    public DbSet<InstrumentAudit> InstrumentAudits => Set<InstrumentAudit>();
    public DbSet<InstrumentIdentifier> InstrumentIdentifiers => Set<InstrumentIdentifier>();
    public DbSet<Issuer> Issuers => Set<Issuer>();
    public DbSet<Sector> Sectors => Set<Sector>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AssetClass>(entity =>
        {
            entity.HasKey(item => item.AssetClassId);
            entity.Property(item => item.AssetClassId).HasMaxLength(32);
            entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasKey(item => item.CurrencyId);
            entity.Property(item => item.CurrencyName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Exchange>(entity =>
        {
            entity.HasKey(item => item.ExchangeId);
            entity.Property(item => item.MicCode).HasMaxLength(12).IsRequired();
            entity.Property(item => item.ExchangeName).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Country).HasMaxLength(80).IsRequired();
            entity.Property(item => item.Timezone).HasMaxLength(80).IsRequired();

            entity.HasOne(item => item.Currency)
                .WithMany(item => item.Exchanges)
                .HasForeignKey(item => item.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IdentifierType>(entity =>
        {
            entity.HasKey(item => item.IdentifierTypeId);
            entity.Property(item => item.IdentifierTypeId).HasMaxLength(32);
            entity.Property(item => item.IdentifierTypeName).HasMaxLength(50).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Instrument>(entity =>
        {
            entity.HasKey(item => item.InstrumentId);
            entity.Property(item => item.InstrumentId).HasMaxLength(40);
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.Property(item => item.PrimaryIsin).HasMaxLength(12).IsRequired();
            entity.Property(item => item.AssetClassId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.Status).HasMaxLength(32).IsRequired();

            entity.HasIndex(item => item.PrimaryIsin).IsUnique();

            entity.HasOne(item => item.AssetClass)
                .WithMany(item => item.Instruments)
                .HasForeignKey(item => item.AssetClassId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Sector)
                .WithMany(item => item.Instruments)
                .HasForeignKey(item => item.SectorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Exchange)
                .WithMany(item => item.Instruments)
                .HasForeignKey(item => item.ExchangeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Currency)
                .WithMany(item => item.Instruments)
                .HasForeignKey(item => item.CurrencyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(item => item.Issuer)
                .WithMany(item => item.Instruments)
                .HasForeignKey(item => item.IssuerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InstrumentAudit>(entity =>
        {
            entity.HasKey(item => item.AuditId);
            entity.Property(item => item.AuditId).HasMaxLength(40);
            entity.Property(item => item.InstrumentId).HasMaxLength(40).IsRequired();
            entity.Property(item => item.ChangedBy).HasMaxLength(100).IsRequired();
            entity.Property(item => item.FieldName).HasMaxLength(100).IsRequired();
            entity.Property(item => item.OldValue).HasMaxLength(256);
            entity.Property(item => item.NewValue).HasMaxLength(256);
            entity.Property(item => item.ChangeSource).HasMaxLength(100).IsRequired();

            entity.HasOne(item => item.Instrument)
                .WithMany(item => item.Audits)
                .HasForeignKey(item => item.InstrumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InstrumentIdentifier>(entity =>
        {
            entity.HasKey(item => item.IdentifierId);
            entity.Property(item => item.IdentifierId).HasMaxLength(40);
            entity.Property(item => item.InstrumentId).HasMaxLength(40).IsRequired();
            entity.Property(item => item.IdentifierTypeId).HasMaxLength(32).IsRequired();
            entity.Property(item => item.IdentifierValue).HasMaxLength(64).IsRequired();

            entity.HasIndex(item => new { item.IdentifierTypeId, item.IdentifierValue }).IsUnique();

            entity.HasOne(item => item.Instrument)
                .WithMany(item => item.Identifiers)
                .HasForeignKey(item => item.InstrumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.IdentifierType)
                .WithMany(item => item.InstrumentIdentifiers)
                .HasForeignKey(item => item.IdentifierTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Issuer>(entity =>
        {
            entity.HasKey(item => item.IssuerId);
            entity.Property(item => item.IssuerName).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<Sector>(entity =>
        {
            entity.HasKey(item => item.SectorId);
            entity.Property(item => item.SectorName).HasMaxLength(100).IsRequired();
        });
    }
}
