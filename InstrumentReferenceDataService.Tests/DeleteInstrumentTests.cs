using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstrumentReferenceDataService.Tests;

public sealed class DeleteInstrumentTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory factory = new();
    private HttpClient client = null!;

    public async Task InitializeAsync()
    {
        client = factory.CreateClient();
        await SeedDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
    }

    [Fact]
    public async Task Delete_WithValidExistingId_ReturnsNoContent()
    {
        var response = await client.DeleteAsync("/api/instruments/TEST-INS-0001");

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithValidExistingId_RemovesInstrumentFromDatabase()
    {
        await client.DeleteAsync("/api/instruments/TEST-INS-0001");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var instrument = await dbContext.Instruments
            .SingleOrDefaultAsync(i => i.InstrumentId == "TEST-INS-0001");

        Assert.Null(instrument);
    }

    [Fact]
    public async Task Delete_WithValidExistingId_CascadesIdentifiers()
    {
        await client.DeleteAsync("/api/instruments/TEST-INS-0001");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var identifiers = await dbContext.InstrumentIdentifiers
            .Where(i => i.InstrumentId == "TEST-INS-0001")
            .ToListAsync();

        Assert.Empty(identifiers);
    }

    [Fact]
    public async Task Delete_WithValidExistingId_CascadesAudits()
    {
        await client.DeleteAsync("/api/instruments/TEST-INS-0001");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var audits = await dbContext.InstrumentAudits
            .Where(a => a.InstrumentId == "TEST-INS-0001")
            .ToListAsync();

        Assert.Empty(audits);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_ReturnsNotFound()
    {
        var response = await client.DeleteAsync("/api/instruments/DOES-NOT-EXIST");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentId_DoesNotAffectOtherInstruments()
    {
        await client.DeleteAsync("/api/instruments/DOES-NOT-EXIST");

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var count = await dbContext.Instruments.CountAsync();

        Assert.Equal(2, count);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Delete_WithWhitespaceId_ReturnsNotFound(string id)
    {
        var response = await client.DeleteAsync($"/api/instruments/{id}");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task SeedDatabaseAsync()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();

        // Clear any pre-existing data
        dbContext.InstrumentAudits.RemoveRange(dbContext.InstrumentAudits);
        dbContext.InstrumentIdentifiers.RemoveRange(dbContext.InstrumentIdentifiers);
        dbContext.Instruments.RemoveRange(dbContext.Instruments);
        await dbContext.SaveChangesAsync();

        // Seed reference data if missing
        if (!await dbContext.AssetClasses.AnyAsync())
        {
            await dbContext.AssetClasses.AddAsync(new AssetClass { AssetClassId = "EQ", Name = "Equity" });
        }

        if (!await dbContext.Sectors.AnyAsync())
        {
            await dbContext.Sectors.AddAsync(new Sector { SectorId = 1, SectorName = "Technology" });
        }

        if (!await dbContext.Currencies.AnyAsync())
        {
            await dbContext.Currencies.AddAsync(new Currency { CurrencyId = 1, CurrencyName = "USD" });
        }

        if (!await dbContext.Issuers.AnyAsync())
        {
            await dbContext.Issuers.AddAsync(new Issuer { IssuerId = 1, IssuerName = "Test Issuer" });
        }

        await dbContext.SaveChangesAsync();

        if (!await dbContext.Exchanges.AnyAsync())
        {
            await dbContext.Exchanges.AddAsync(new Exchange
            {
                ExchangeId = 1,
                MicCode = "XNYS",
                ExchangeName = "NYSE",
                Country = "United States",
                Timezone = "America/New_York",
                CurrencyId = 1
            });
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.IdentifierTypes.AnyAsync())
        {
            await dbContext.IdentifierTypes.AddAsync(new IdentifierType
            {
                IdentifierTypeId = "ISIN",
                IdentifierTypeName = "ISIN",
                Description = "International Securities Identification Number"
            });
            await dbContext.SaveChangesAsync();
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var instrument1 = new Instrument
        {
            InstrumentId = "TEST-INS-0001",
            Name = "Test Instrument One",
            PrimaryIsin = "US0000000001",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = today,
            LastUpdated = today
        };

        var instrument2 = new Instrument
        {
            InstrumentId = "TEST-INS-0002",
            Name = "Test Instrument Two",
            PrimaryIsin = "US0000000002",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = today,
            LastUpdated = today
        };

        await dbContext.Instruments.AddRangeAsync(instrument1, instrument2);
        await dbContext.SaveChangesAsync();

        await dbContext.InstrumentIdentifiers.AddAsync(new InstrumentIdentifier
        {
            IdentifierId = "ID-ISIN-TEST-INS-0001",
            InstrumentId = "TEST-INS-0001",
            IdentifierTypeId = "ISIN",
            IdentifierValue = "US0000000001",
            EffectiveDate = today
        });

        await dbContext.InstrumentAudits.AddAsync(new InstrumentAudit
        {
            AuditId = "AUDIT-TEST-INS-0001",
            InstrumentId = "TEST-INS-0001",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = "test.user",
            FieldName = "status",
            OldValue = null,
            NewValue = "Active",
            ChangeSource = "TestSeed"
        });

        await dbContext.SaveChangesAsync();
    }
}
