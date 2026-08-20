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
        var primaryIsin = BuildUniqueIsin(token, '1');
        dbContext.Instruments.Add(CreateInstrument(instrumentId, primaryIsin));
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Updated Name",
            PrimaryIsin = primaryIsin,
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
        var primaryIsin = BuildUniqueIsin(token, '2');
        dbContext.Instruments.Add(CreateInstrument(instrumentId, primaryIsin));
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Renamed Instrument",
            PrimaryIsin = primaryIsin,
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
            PrimaryIsin = BuildUniqueIsin(NextToken(), '4'),
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
        var primaryIsin = BuildUniqueIsin(token, '3');
        dbContext.Instruments.Add(CreateInstrument(instrumentId, primaryIsin));
        dbContext.InstrumentIdentifiers.Add(new InstrumentIdentifier
        {
            IdentifierId = $"ID-ISIN-{instrumentId}",
            InstrumentId = instrumentId,
            IdentifierTypeId = "ISIN",
            IdentifierValue = primaryIsin,
            EffectiveDate = new DateOnly(2026, 1, 1),
        });
        await dbContext.SaveChangesAsync();

        var beforeCount = await dbContext.InstrumentAudits
            .AsNoTracking()
            .CountAsync(item => item.InstrumentId == instrumentId);

        var request = new
        {
            Name = $"Instrument {instrumentId}",
            PrimaryIsin = primaryIsin,
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

    [Fact]
    public async Task PutInstrument_UpdatesIdentifiers_AddsAndRemovesOptionalTypes()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-UPD-{token}";
        var originalIsin = BuildUniqueIsin(token, '5');
        var updatedIsin = BuildUniqueIsin(token, '6');

        dbContext.Instruments.Add(CreateInstrument(instrumentId, originalIsin));
        dbContext.InstrumentIdentifiers.AddRange(
            new InstrumentIdentifier
            {
                IdentifierId = $"ID-ISIN-{instrumentId}",
                InstrumentId = instrumentId,
                IdentifierTypeId = "ISIN",
                IdentifierValue = originalIsin,
                EffectiveDate = new DateOnly(2026, 1, 1),
            },
            new InstrumentIdentifier
            {
                IdentifierId = $"ID-CUSIP-{instrumentId}",
                InstrumentId = instrumentId,
                IdentifierTypeId = "CUSIP",
                IdentifierValue = "LS9BSD30F",
                EffectiveDate = new DateOnly(2026, 1, 1),
            });
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Renamed Instrument",
            PrimaryIsin = updatedIsin,
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Pending",
            EffectiveDate = new DateOnly(2026, 3, 10),
            AdditionalIdentifiers = new[]
            {
                new { IdentifierTypeId = "RIC", IdentifierValue = "XETR.5NWWCP" }
            }
        };

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/instruments/{instrumentId}", request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var identifiers = await dbContext.InstrumentIdentifiers
            .AsNoTracking()
            .Where(item => item.InstrumentId == instrumentId)
            .ToListAsync();

        Assert.Contains(identifiers, item => item.IdentifierTypeId == "ISIN" && item.IdentifierValue == updatedIsin);
        Assert.DoesNotContain(identifiers, item => item.IdentifierTypeId == "CUSIP");
        Assert.Contains(identifiers, item => item.IdentifierTypeId == "RIC" && item.IdentifierValue == "XETR.5NWWCP");
    }

    [Fact]
    public async Task PutInstrument_WithInvalidAdditionalIdentifierFormat_ReturnsBadRequest()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-UPD-{token}";
        var primaryIsin = BuildUniqueIsin(token, '7');
        dbContext.Instruments.Add(CreateInstrument(instrumentId, primaryIsin));
        dbContext.InstrumentIdentifiers.Add(new InstrumentIdentifier
        {
            IdentifierId = $"ID-ISIN-{instrumentId}",
            InstrumentId = instrumentId,
            IdentifierTypeId = "ISIN",
            IdentifierValue = primaryIsin,
            EffectiveDate = new DateOnly(2026, 1, 1),
        });
        await dbContext.SaveChangesAsync();

        var request = new
        {
            Name = "Updated Name",
            PrimaryIsin = primaryIsin,
            AssetClassId = "EQ",
            SectorId = 1,
            ExchangeId = 1,
            CurrencyId = 1,
            IssuerId = 1,
            Status = "Active",
            EffectiveDate = new DateOnly(2026, 2, 1),
            AdditionalIdentifiers = new[]
            {
                new { IdentifierTypeId = "CUSIP", IdentifierValue = "BAD" }
            }
        };

        var client = factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/instruments/{instrumentId}", request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid CUSIP identifier format", body, StringComparison.OrdinalIgnoreCase);
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

            if (!await dbContext.IdentifierTypes.AnyAsync(item => item.IdentifierTypeId == "ISIN"))
            {
                dbContext.IdentifierTypes.Add(new IdentifierType
                {
                    IdentifierTypeId = "ISIN",
                    IdentifierTypeName = "International Securities Identification Number"
                });
            }

            if (!await dbContext.IdentifierTypes.AnyAsync(item => item.IdentifierTypeId == "CUSIP"))
            {
                dbContext.IdentifierTypes.Add(new IdentifierType
                {
                    IdentifierTypeId = "CUSIP",
                    IdentifierTypeName = "Committee on Uniform Securities Identification Procedures"
                });
            }

            if (!await dbContext.IdentifierTypes.AnyAsync(item => item.IdentifierTypeId == "RIC"))
            {
                dbContext.IdentifierTypes.Add(new IdentifierType
                {
                    IdentifierTypeId = "RIC",
                    IdentifierTypeName = "Refinitiv Instrument Code"
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
