using System.Net;
using System.Text;
using System.Text.Json;
using System.Linq;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.AspNetCore.Mvc.Testing;
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

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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

        var failingItem = Assert.Single(result.Where(item => item.InstrumentId == failingInstrumentId));

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
}