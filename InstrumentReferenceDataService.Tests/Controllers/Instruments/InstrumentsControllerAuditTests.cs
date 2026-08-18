using System.Net;
using System.Net.Http.Json;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstrumentReferenceDataService.Tests.Controllers.Instruments;

public sealed class InstrumentsControllerAuditTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly SemaphoreSlim SeedLock = new(1, 1);
    private static int uniqueCounter = Environment.TickCount & 0x3FFFFFFF;
    private readonly TestWebApplicationFactory factory;

    public InstrumentsControllerAuditTests(TestWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task GetAuditByInstrumentId_ReturnsOrderedAuditRecords_ForExistingInstrument()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-AUD-{token}-001";
        var otherInstrumentId = $"INS-AUD-{token}-002";
        var newAuditId = $"AUD-{instrumentId}-NEW";
        var oldAuditId = $"AUD-{instrumentId}-OLD";
        var otherAuditId = $"AUD-{otherInstrumentId}-ONLY";

        dbContext.Instruments.AddRange(
            CreateInstrument(instrumentId, BuildUniqueIsin(token, '1')),
            CreateInstrument(otherInstrumentId, BuildUniqueIsin(token, '2')));

        var olderChangedAt = new DateTime(2026, 1, 10, 8, 30, 0, DateTimeKind.Utc);
        var newerChangedAt = new DateTime(2026, 1, 11, 10, 0, 0, DateTimeKind.Utc);

        dbContext.InstrumentAudits.AddRange(
            new InstrumentAudit
            {
                AuditId = oldAuditId,
                InstrumentId = instrumentId,
                ChangedAt = olderChangedAt,
                ChangedBy = "ops.user",
                FieldName = "status",
                OldValue = "Pending",
                NewValue = "Active",
                ChangeSource = "ManualReview"
            },
            new InstrumentAudit
            {
                AuditId = newAuditId,
                InstrumentId = instrumentId,
                ChangedAt = newerChangedAt,
                ChangedBy = "reference.admin",
                FieldName = "exchange_id",
                OldValue = "XNAS",
                NewValue = "XNYS",
                ChangeSource = "BackfillJob"
            },
            new InstrumentAudit
            {
                AuditId = otherAuditId,
                InstrumentId = otherInstrumentId,
                ChangedAt = new DateTime(2026, 1, 12, 9, 0, 0, DateTimeKind.Utc),
                ChangedBy = "system.seed",
                FieldName = "currency_id",
                OldValue = "USD",
                NewValue = "EUR",
                ChangeSource = "MockGenerator"
            });

        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/instruments/{instrumentId}/audit");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentAuditResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);

      
        Assert.Equal(newerChangedAt, payload[0].ChangedAt);
        Assert.Equal(olderChangedAt, payload[1].ChangedAt);

        // Verify important response fields are projected correctly.
        Assert.Equal("reference.admin", payload[0].ChangedBy);
        Assert.Equal("exchange_id", payload[0].FieldName);
        Assert.Equal("XNAS", payload[0].OldValue);
        Assert.Equal("XNYS", payload[0].NewValue);
        Assert.Equal("BackfillJob", payload[0].ChangeSource);

        Assert.Equal("ops.user", payload[1].ChangedBy);
        Assert.Equal("status", payload[1].FieldName);
        Assert.Equal("Pending", payload[1].OldValue);
        Assert.Equal("Active", payload[1].NewValue);
        Assert.Equal("ManualReview", payload[1].ChangeSource);
    }

    [Fact]
    public async Task GetAuditByInstrumentId_ReturnsEmptyCollection_ForExistingInstrumentWithoutAuditRecords()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-AUD-EMPTY-{token}";
        dbContext.Instruments.Add(CreateInstrument(instrumentId, BuildUniqueIsin(token, '3')));
        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/instruments/{instrumentId}/audit");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentAuditResponse>>();
        Assert.NotNull(payload);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task GetAuditByInstrumentId_ResolvesReferenceIdsToReadableValues()
    {
        // Arrange
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedRequiredReferenceDataAsync(dbContext);

        var token = NextToken();
        var instrumentId = $"INS-AUD-MEANINGFUL-{token}";
        dbContext.Instruments.Add(CreateInstrument(instrumentId, BuildUniqueIsin(token, '4')));

        var changedAt = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        dbContext.InstrumentAudits.Add(new InstrumentAudit
        {
            AuditId = $"AUD-{instrumentId}-SECTOR",
            InstrumentId = instrumentId,
            ChangedAt = changedAt,
            ChangedBy = "reference.admin",
            FieldName = "sector_id",
            OldValue = "1",
            NewValue = "2",
            ChangeSource = "BackfillJob"
        });

        await dbContext.SaveChangesAsync();

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/instruments/{instrumentId}/audit");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentAuditResponse>>();
        Assert.NotNull(payload);
        var audit = Assert.Single(payload);
        Assert.Equal("sector_id", audit.FieldName);
        Assert.Equal("Technology (1)", audit.OldValue);
        Assert.Equal("Healthcare (2)", audit.NewValue);
    }

    [Fact]
    public async Task GetAuditByInstrumentId_ReturnsNotFound_ForNonexistentInstrument()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var missingInstrumentId = $"INS-DOES-NOT-EXIST-{NextToken()}";
        var response = await client.GetAsync($"/api/instruments/{missingInstrumentId}/audit");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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

            if (!await dbContext.Sectors.AnyAsync(item => item.SectorId == 2))
            {
                dbContext.Sectors.Add(new Sector
                {
                    SectorId = 2,
                    SectorName = "Healthcare"
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
        // ISIN max length is 12 in the model: 2 country chars + 9 token chars + 1 suffix.
        var core = token.Length >= 9 ? token[^9..] : token.PadLeft(9, '0');
        return $"US{core}{suffix}";
    }
}
