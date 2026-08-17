using System.Net;
using System.Net.Http.Json;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstrumentReferenceDataService.Tests.Controllers.Instruments;

public sealed class InstrumentsControllerUpdateTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static int uniqueCounter = Environment.TickCount & 0x3FFFFFFF;
    private readonly TestWebApplicationFactory factory;

    public InstrumentsControllerUpdateTests(TestWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PutInstrument_ReturnsNoContent_ForExistingInstrument()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-UPD-{token}";
        dbContext.Instruments.Add(CreateInstrument(instrumentId, BuildUniqueIsin(token, '1')));
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Updated Name",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Suspended",
            EffectiveDate = new DateOnly(2026, 2, 1)
        };

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/instruments/{instrumentId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutInstrument_PersistsUpdatedValues_AndCreatesAuditEntries()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-UPD-{token}";
        dbContext.Instruments.Add(CreateInstrument(instrumentId, BuildUniqueIsin(token, '2')));
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Renamed Instrument",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Pending",
            EffectiveDate = new DateOnly(2026, 3, 10)
        };

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/instruments/{instrumentId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await dbContext.Instruments
            .AsNoTracking()
            .SingleAsync(item => item.InstrumentId == instrumentId);

        Assert.Equal("Renamed Instrument", updated.Name);
        Assert.Equal("Pending", updated.Status);
        Assert.Equal(new DateOnly(2026, 3, 10), updated.EffectiveDate);

        var audits = await dbContext.InstrumentAudits
            .AsNoTracking()
            .Where(item => item.InstrumentId == instrumentId)
            .OrderBy(item => item.ChangedAt)
            .ToListAsync();

        Assert.True(audits.Count >= 3);

        var nameAudit = audits.Single(item => item.FieldName == "name");
        Assert.Equal($"Instrument {instrumentId}", nameAudit.OldValue);
        Assert.Equal("Renamed Instrument", nameAudit.NewValue);

        var statusAudit = audits.Single(item => item.FieldName == "status");
        Assert.Equal("Active", statusAudit.OldValue);
        Assert.Equal("Pending", statusAudit.NewValue);

        var effectiveDateAudit = audits.Single(item => item.FieldName == "effective_date");
        Assert.Equal("2026-01-01", effectiveDateAudit.OldValue);
        Assert.Equal("2026-03-10", effectiveDateAudit.NewValue);
    }

    [Fact]
    public async Task PutInstrument_ReturnsNotFound_ForNonExistingInstrument()
    {
        var request = new
        {
            Name = "Updated Name",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = new DateOnly(2026, 2, 1)
        };

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/instruments/INS-MISSING-{NextToken()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutInstrument_DoesNotCreateAudit_WhenValuesAreUnchanged()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-UPD-{token}";
        dbContext.Instruments.Add(CreateInstrument(instrumentId, BuildUniqueIsin(token, '3')));
        await dbContext.SaveChangesAsync();

        var beforeCount = await dbContext.InstrumentAudits
            .AsNoTracking()
            .CountAsync(item => item.InstrumentId == instrumentId);

        var request = new
        {
            Name = $"Instrument {instrumentId}",
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = new DateOnly(2026, 1, 1)
        };

        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync($"/api/instruments/{instrumentId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var afterCount = await dbContext.InstrumentAudits
            .AsNoTracking()
            .CountAsync(item => item.InstrumentId == instrumentId);

        Assert.Equal(beforeCount, afterCount);
    }

    private static async Task SeedRequiredReferenceDataAsync(AppDbContext dbContext)
    {
        await SeedLock.WaitAsync();
        try
        {
            if (!await dbContext.AssetClasses.AnyAsync(item => item.AssetClassId == "EQ"))
            {
                dbContext.AssetClasses.Add(new AssetClass
                {
                    AssetClassId = "EQ",
                    Name = "Equity",
                    Description = "Equity instruments"
                });
            }

            if (!await dbContext.Sectors.AnyAsync(item => item.SectorId == 1))
            {
                dbContext.Sectors.Add(new Sector
                {
                    SectorId = 1,
                    SectorName = "Technology"
                });
            }

            if (!await dbContext.Currencies.AnyAsync(item => item.CurrencyId == 1))
            {
                dbContext.Currencies.Add(new Currency
                {
                    CurrencyId = 1,
                    CurrencyName = "USD"
                });
            }

            if (!await dbContext.Issuers.AnyAsync(item => item.IssuerId == 1))
            {
                dbContext.Issuers.Add(new Issuer
                {
                    IssuerId = 1,
                    IssuerName = "Acme Issuer"
                });
            }

            if (!await dbContext.Exchanges.AnyAsync(item => item.ExchangeId == 1))
            {
                dbContext.Exchanges.Add(new Exchange
                {
                    ExchangeId = 1,
                    MicCode = "XNYS",
                    ExchangeName = "New York Stock Exchange",
                    Country = "United States",
                    Timezone = "America/New_York",
                    CurrencyId = 1
                });
            }

            await dbContext.SaveChangesAsync();
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static Instrument CreateInstrument(string instrumentId, string primaryIsin)
    {
        return new Instrument
        {
            InstrumentId = instrumentId,
            Name = $"Instrument {instrumentId}",
            PrimaryIsin = primaryIsin,
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = new DateOnly(2026, 1, 1),
            LastUpdated = new DateOnly(2026, 1, 15)
        };
    }

    private static string NextToken()
    {
        return Interlocked.Increment(ref uniqueCounter).ToString("D10");
    }

    private static string BuildUniqueIsin(string token, char suffix)
    {
        var core = token.Length >= 9 ? token[^9..] : token.PadLeft(9, '0');
        return $"US{core}{suffix}";
    }
}
