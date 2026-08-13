using System.Net;
using System.Net.Http.Json;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstrumentReferenceDataService.Tests;

public class InstrumentsControllerTests
{
    [Fact]
    public async Task GetInstruments_WithoutQueryParams_ReturnsAllInstruments()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentDetailResponse>>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload.Count);
        Assert.Contains(payload, item => item.Instrument.InstrumentId == "INS-1");
        Assert.Contains(payload, item => item.Instrument.InstrumentId == "INS-2");
    }

    [Fact]
    public async Task GetInstruments_WithIsinQueryParam_FiltersByIsin()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments?isin=US0000000001");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentDetailResponse>>();
        Assert.NotNull(payload);
        var instrument = Assert.Single(payload);
        Assert.Equal("INS-1", instrument.Instrument.InstrumentId);
    }

    [Fact]
    public async Task GetInstruments_WithCusipQueryParam_FiltersByCusip()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments?cusip=000000002");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<List<InstrumentDetailResponse>>();
        Assert.NotNull(payload);
        var instrument = Assert.Single(payload);
        Assert.Equal("INS-2", instrument.Instrument.InstrumentId);
    }

    [Fact]
    public async Task GetInstrumentById_WhenInstrumentExists_ReturnsInstrument()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments/INS-1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<InstrumentDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal("INS-1", payload.Instrument.InstrumentId);
    }

    [Fact]
    public async Task GetInstrumentById_WhenInstrumentDoesNotExist_ReturnsNotFound()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments/DOES-NOT-EXIST");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.InstrumentAudits.ExecuteDeleteAsync();
        await dbContext.InstrumentIdentifiers.ExecuteDeleteAsync();
        await dbContext.Instruments.ExecuteDeleteAsync();
        await dbContext.Exchanges.ExecuteDeleteAsync();
        await dbContext.Issuers.ExecuteDeleteAsync();
        await dbContext.Sectors.ExecuteDeleteAsync();
        await dbContext.Currencies.ExecuteDeleteAsync();
        await dbContext.AssetClasses.ExecuteDeleteAsync();
        await dbContext.IdentifierTypes.ExecuteDeleteAsync();

        dbContext.AssetClasses.Add(new AssetClass
        {
            AssetClassId = "EQ",
            Name = "Equity"
        });

        dbContext.Currencies.Add(new Currency
        {
            CurrencyId = 1,
            CurrencyName = "USD"
        });

        dbContext.Sectors.Add(new Sector
        {
            SectorId = 1,
            SectorName = "Technology"
        });

        dbContext.Exchanges.Add(new Exchange
        {
            ExchangeId = 1,
            MicCode = "XNAS",
            ExchangeName = "NASDAQ",
            Country = "US",
            Timezone = "America/New_York",
            CurrencyId = 1
        });

        dbContext.Issuers.Add(new Issuer
        {
            IssuerId = 1,
            IssuerName = "Contoso"
        });

        dbContext.IdentifierTypes.AddRange(
            new IdentifierType
            {
                IdentifierTypeId = "ISIN",
                IdentifierTypeName = "International Securities Identification Number"
            },
            new IdentifierType
            {
                IdentifierTypeId = "CUSIP",
                IdentifierTypeName = "Committee on Uniform Securities Identification Procedures"
            });

        dbContext.Instruments.AddRange(
            new Instrument
            {
                InstrumentId = "INS-1",
                Name = "Contoso Equity A",
                PrimaryIsin = "US0000000001",
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = new DateOnly(2024, 1, 1),
                LastUpdated = new DateOnly(2024, 1, 15)
            },
            new Instrument
            {
                InstrumentId = "INS-2",
                Name = "Contoso Equity B",
                PrimaryIsin = "US0000000002",
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = new DateOnly(2024, 2, 1),
                LastUpdated = new DateOnly(2024, 2, 15)
            });

        dbContext.InstrumentIdentifiers.AddRange(
            new InstrumentIdentifier
            {
                IdentifierId = "ID-1",
                InstrumentId = "INS-1",
                IdentifierTypeId = "ISIN",
                IdentifierValue = "US0000000001",
                EffectiveDate = new DateOnly(2024, 1, 1)
            },
            new InstrumentIdentifier
            {
                IdentifierId = "ID-2",
                InstrumentId = "INS-1",
                IdentifierTypeId = "CUSIP",
                IdentifierValue = "000000001",
                EffectiveDate = new DateOnly(2024, 1, 1)
            },
            new InstrumentIdentifier
            {
                IdentifierId = "ID-3",
                InstrumentId = "INS-2",
                IdentifierTypeId = "ISIN",
                IdentifierValue = "US0000000002",
                EffectiveDate = new DateOnly(2024, 2, 1)
            },
            new InstrumentIdentifier
            {
                IdentifierId = "ID-4",
                InstrumentId = "INS-2",
                IdentifierTypeId = "CUSIP",
                IdentifierValue = "000000002",
                EffectiveDate = new DateOnly(2024, 2, 1)
            });

        dbContext.InstrumentAudits.AddRange(
            new InstrumentAudit
            {
                AuditId = "AUD-1",
                InstrumentId = "INS-1",
                ChangedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                ChangedBy = "test-user",
                FieldName = "Status",
                OldValue = "Pending",
                NewValue = "Active",
                ChangeSource = "UnitTest"
            },
            new InstrumentAudit
            {
                AuditId = "AUD-2",
                InstrumentId = "INS-2",
                ChangedAt = new DateTime(2024, 2, 15, 10, 0, 0, DateTimeKind.Utc),
                ChangedBy = "test-user",
                FieldName = "Status",
                OldValue = "Pending",
                NewValue = "Active",
                ChangeSource = "UnitTest"
            });

        await dbContext.SaveChangesAsync();
    }
}
