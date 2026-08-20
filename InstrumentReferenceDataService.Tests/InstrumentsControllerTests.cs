using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Controllers;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InstrumentReferenceDataService.Tests;

public sealed class InstrumentsControllerTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory webApplicationFactory;
    private HttpClient httpClient = null!;

    public InstrumentsControllerTests()
    {
        webApplicationFactory = new TestWebApplicationFactory();
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        httpClient = webApplicationFactory.CreateClient();
        await Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        httpClient.Dispose();
        await webApplicationFactory.DisposeAsync();
    }

    [Fact]
    public async Task GetById_WithExistingInstrument_ReturnsOkAndInstrumentDetail()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Get Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var response = await httpClient.GetAsync($"/api/instruments/{instrumentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<InstrumentDetailResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(result);
        Assert.Equal(instrumentId, result.Instrument.InstrumentId);
        Assert.Equal("Get Test Instrument", result.Instrument.Name);
        Assert.Equal(primaryIsin, result.Instrument.PrimaryIsin);
        Assert.Equal("EQ", result.Instrument.AssetClassId);
    }

    [Fact]
    public async Task GetById_WithNonExistentInstrument_ReturnsNotFound()
    {
        var nonExistentId = "INS-UNKNOWN-999";

        var response = await httpClient.GetAsync($"/api/instruments/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyInstrumentId_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentAssetClass_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-AC",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "NONEXISTENT",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentSector_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-SEC",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "EQ",
            99999,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentExchange_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-EXC",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "EQ",
            1,
            99999,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentCurrency_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-CUR",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "EQ",
            1,
            1,
            99999,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithNonExistentIssuer_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-ISS",
            "Test Instrument",
            $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1",
            "EQ",
            1,
            1,
            1,
            99999,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithInvalidIsinFormat_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var request = new CreateInstrumentRequest(
            "INS-BAD-ISIN",
            "Test Instrument",
            "INVALID",
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDuplicateIsinInIdentifiers_ReturnsConflict()
    {
        await SeedReferenceDataAsync();

        string existingIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";
        string identifierIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}2";
        string uniqueInstrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = uniqueInstrumentId,
                Name = "Existing Instrument",
                PrimaryIsin = existingIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            dbContext.InstrumentIdentifiers.Add(new InstrumentIdentifier
            {
                IdentifierId = $"ID-{uniqueInstrumentId}",
                InstrumentId = uniqueInstrumentId,
                IdentifierTypeId = "ISIN",
                IdentifierValue = identifierIsin,
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new CreateInstrumentRequest(
            $"NEW-{uniqueInstrumentId}",
            "New Instrument",
            identifierIsin,
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);
        var error = await response.Content.ReadAsStringAsync();

        Assert.Equal("An instrument with this ISIN already exists", error.Trim());
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithAdditionalIdentifiers_PersistsIsInAndAdditionalIdentifiers()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        var request = new CreateInstrumentRequest(
            instrumentId,
            "Identifier Test Instrument",
            primaryIsin,
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow),
            [
                new AdditionalIdentifierInput("CUSIP", "LS9BSD30F"),
                new AdditionalIdentifierInput("RIC", "XETR.5NWWCP"),
                new AdditionalIdentifierInput("SEDOL", "BD5L398"),
                new AdditionalIdentifierInput("TICKER", "HELIYH27")
            ]
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await httpClient.GetFromJsonAsync<InstrumentDetailResponse>($"/api/instruments/{instrumentId}");
        Assert.NotNull(detail);
        Assert.Contains(detail.Identifiers, id => id.IdentifierTypeId == "ISIN" && id.IdentifierValue == primaryIsin);
        Assert.Contains(detail.Identifiers, id => id.IdentifierTypeId == "CUSIP" && id.IdentifierValue == "LS9BSD30F");
        Assert.Contains(detail.Identifiers, id => id.IdentifierTypeId == "RIC" && id.IdentifierValue == "XETR.5NWWCP");
        Assert.Contains(detail.Identifiers, id => id.IdentifierTypeId == "SEDOL" && id.IdentifierValue == "BD5L398");
        Assert.Contains(detail.Identifiers, id => id.IdentifierTypeId == "TICKER" && id.IdentifierValue == "HELIYH27");
    }

    [Fact]
    public async Task Create_WithInvalidAdditionalIdentifierFormat_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        var request = new CreateInstrumentRequest(
            instrumentId,
            "Invalid Identifier Format Instrument",
            primaryIsin,
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new AdditionalIdentifierInput("CUSIP", "BAD")]
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);
        var error = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid CUSIP identifier format", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_WithUnknownAdditionalIdentifierType_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        var request = new CreateInstrumentRequest(
            instrumentId,
            "Bad Identifier Type Instrument",
            primaryIsin,
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow),
            [new AdditionalIdentifierInput("NONEXISTENT", "VALUE")]
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PostAsync("/api/instruments", jsonContent);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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
    public async Task GetEditOptions_ReturnsReferenceOptionsAndStatuses()
    {
        using var factory = new TestWebApplicationFactory();
        await SeedAsync(factory.Services);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/instruments/options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<InstrumentEditOptionsResponse>();
        Assert.NotNull(payload);

        Assert.Contains(payload.AssetClasses, item => item.AssetClassId == "EQ");
        Assert.Contains(payload.Sectors, item => item.SectorId == 1);
        Assert.Contains(payload.Exchanges, item => item.ExchangeId == 1);
        Assert.Contains(payload.Currencies, item => item.CurrencyId == 1);
        Assert.Contains(payload.Issuers, item => item.IssuerId == 1);
        Assert.Contains(payload.Statuses, item => string.Equals(item.Value, "Active", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.Statuses, item => string.Equals(item.Value, "Pending", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(payload.IdentifierTypes, item => item.IdentifierTypeId == "ISIN");
        Assert.Contains(payload.IdentifierTypes, item => item.IdentifierTypeId == "CUSIP");
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

    [Fact]
    public async Task GetQualityReport_ReturnsOnlyInstrumentsWithFailingIndicators()
    {
        await SeedReferenceDataAsync();

        var failingInstrumentId = $"INS-FAIL-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        var passingInstrumentId = $"INS-PASS-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

        var failingIsin = $"XX{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}X";
        var passingIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";
        var failingIdentifierIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}9";

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.AddRange(
                new Instrument
                {
                    InstrumentId = failingInstrumentId,
                    Name = "Failing Quality Instrument",
                    PrimaryIsin = failingIsin,
                    AssetClassId = "EQ",
                    SectorId = 1,
                    ExchangeId = 1,
                    CurrencyId = 1,
                    IssuerId = 1,
                    Status = "Active",
                    EffectiveDate = today,
                    LastUpdated = today.AddDays(-1)
                },
                new Instrument
                {
                    InstrumentId = passingInstrumentId,
                    Name = "Passing Quality Instrument",
                    PrimaryIsin = passingIsin,
                    AssetClassId = "EQ",
                    SectorId = 1,
                    ExchangeId = 1,
                    CurrencyId = 1,
                    IssuerId = 1,
                    Status = "Active",
                    EffectiveDate = today,
                    LastUpdated = today
                });

            dbContext.InstrumentIdentifiers.AddRange(
                new InstrumentIdentifier
                {
                    IdentifierId = $"ID-BAD-RANGE-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                    InstrumentId = failingInstrumentId,
                    IdentifierTypeId = "ISIN",
                    IdentifierValue = failingIdentifierIsin,
                    EffectiveDate = today,
                    ExpiryDate = today.AddDays(-1)
                },
                new InstrumentIdentifier
                {
                    IdentifierId = $"ID-GOOD-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}",
                    InstrumentId = passingInstrumentId,
                    IdentifierTypeId = "ISIN",
                    IdentifierValue = passingIsin,
                    EffectiveDate = today,
                    ExpiryDate = null
                });

            await dbContext.SaveChangesAsync();
        }

        var response = await httpClient.GetAsync("/api/instruments/quality-report");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<InstrumentQualityReportItemResponse>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(result);

        var failingItem = Assert.Single(result, item => item.InstrumentId == failingInstrumentId);

        Assert.Equal("Failing Quality Instrument", failingItem.Name);
        Assert.Contains(failingItem.FailingIndicators, indicator => indicator.Code == "PRIMARY_ISIN_FORMAT_INVALID");
        Assert.Contains(failingItem.FailingIndicators, indicator => indicator.Code == "EFFECTIVE_DATE_AFTER_LAST_UPDATED");
        Assert.Contains(failingItem.FailingIndicators, indicator => indicator.Code == "PRIMARY_ISIN_IDENTIFIER_MISSING");
        Assert.Contains(failingItem.FailingIndicators, indicator => indicator.Code == "IDENTIFIER_DATE_RANGE_INVALID");
        Assert.DoesNotContain(result, item => item.InstrumentId == passingInstrumentId);
    }

    private async Task SeedReferenceDataAsync()
    {
        await using var scope = webApplicationFactory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.AssetClasses.Any())
        {
            return;
        }

        dbContext.AssetClasses.AddRange(
            new AssetClass { AssetClassId = "EQ", Name = "Equity" },
            new AssetClass { AssetClassId = "FI", Name = "Fixed Income" },
            new AssetClass { AssetClassId = "FX", Name = "Foreign Exchange" }
        );

        dbContext.IdentifierTypes.Add(new IdentifierType
        {
            IdentifierTypeId = "ISIN",
            IdentifierTypeName = "International Securities Identification Number"
        });

        dbContext.IdentifierTypes.AddRange(
            new IdentifierType
            {
                IdentifierTypeId = "CUSIP",
                IdentifierTypeName = "Committee on Uniform Securities Identification Procedures"
            },
            new IdentifierType
            {
                IdentifierTypeId = "RIC",
                IdentifierTypeName = "Refinitiv Instrument Code"
            },
            new IdentifierType
            {
                IdentifierTypeId = "SEDOL",
                IdentifierTypeName = "Stock Exchange Daily Official List"
            },
            new IdentifierType
            {
                IdentifierTypeId = "TICKER",
                IdentifierTypeName = "Exchange Ticker Symbol"
            });

        dbContext.Currencies.Add(new Currency { CurrencyId = 1, CurrencyName = "USD" });
        dbContext.Sectors.Add(new Sector { SectorId = 1, SectorName = "Technology" });

        dbContext.Exchanges.Add(new Exchange
        {
            ExchangeId = 1,
            MicCode = "XETRA",
            ExchangeName = "Xetra",
            Country = "DE",
            Timezone = "CET",
            CurrencyId = 1
        });

        dbContext.Issuers.Add(new Issuer { IssuerId = 1, IssuerName = "Test Issuer" });

        await dbContext.SaveChangesAsync();
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
            },
            new IdentifierType
            {
                IdentifierTypeId = "RIC",
                IdentifierTypeName = "Refinitiv Instrument Code"
            },
            new IdentifierType
            {
                IdentifierTypeId = "SEDOL",
                IdentifierTypeName = "Stock Exchange Daily Official List"
            },
            new IdentifierType
            {
                IdentifierTypeId = "TICKER",
                IdentifierTypeName = "Exchange Ticker Symbol"
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

    [Fact]
    public async Task Update_WithNonExistentAssetClass_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "NONEXISTENT",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNonExistentSector_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "EQ",
            99999,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNonExistentExchange_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "EQ",
            1,
            99999,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNonExistentCurrency_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "EQ",
            1,
            1,
            99999,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithNonExistentIssuer_ReturnsBadRequest()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "EQ",
            1,
            1,
            1,
            99999,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidIds_ReturnsNoContent()
    {
        await SeedReferenceDataAsync();

        var instrumentId = $"INS-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var primaryIsin = $"US{Guid.NewGuid().ToString("N").Substring(0, 9).ToUpper()}1";

        await using (var scope = webApplicationFactory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            dbContext.Instruments.Add(new Instrument
            {
                InstrumentId = instrumentId,
                Name = "Update Test Instrument",
                PrimaryIsin = primaryIsin,
                AssetClassId = "EQ",
                SectorId = 1,
                ExchangeId = 1,
                CurrencyId = 1,
                IssuerId = 1,
                Status = "Active",
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                LastUpdated = DateOnly.FromDateTime(DateTime.UtcNow)
            });

            await dbContext.SaveChangesAsync();
        }

        var request = new UpdateInstrumentRequest(
            "Updated Name",
            primaryIsin,
            "EQ",
            1,
            1,
            1,
            1,
            "Active",
            DateOnly.FromDateTime(DateTime.UtcNow)
        );

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await httpClient.PutAsync($"/api/instruments/{instrumentId}", jsonContent);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}