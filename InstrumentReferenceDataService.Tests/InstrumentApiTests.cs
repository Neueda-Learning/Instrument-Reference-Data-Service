using System.Net;
using System.Net.Http.Json;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Services;
using Xunit;

namespace InstrumentReferenceDataService.Tests;

public sealed class InstrumentApiTests
{
    [Fact]
    public async Task GenerateEndpoint_CreatesDeterministicData()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/api/mock-data/generate?count=3&seed=123", content: null);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MockDataGenerationResult>();

        Assert.NotNull(result);
        Assert.Equal(3, result.InstrumentsCreated);
        Assert.Equal(15, result.InstrumentIdentifiersCreated);
        Assert.Equal(123, result.SeedUsed);
    }

    [Fact]
    public async Task InstrumentEndpoints_ReturnListDetailIdentifiersAndAudits()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsync("/api/mock-data/generate?count=4&seed=456", content: null);

        var listResponse = await client.GetFromJsonAsync<InstrumentListResponse>("/api/instruments?take=10");

        Assert.NotNull(listResponse);
        Assert.True(listResponse.TotalCount >= 4);
        Assert.True(listResponse.Items.Count >= 4);
        var instrument = listResponse.Items[0];

        var detailResponse = await client.GetFromJsonAsync<InstrumentDetailResponse>($"/api/instruments/{instrument.InstrumentId}");

        Assert.NotNull(detailResponse);
        Assert.Equal(instrument.InstrumentId, detailResponse.Instrument.InstrumentId);
        Assert.Equal(5, detailResponse.Identifiers.Count);
        Assert.NotEmpty(detailResponse.Audits);

        var identifiersResponse = await client.GetFromJsonAsync<List<InstrumentIdentifierResponse>>($"/api/instruments/{instrument.InstrumentId}/identifiers");
        var auditsResponse = await client.GetFromJsonAsync<List<InstrumentAuditResponse>>($"/api/instruments/{instrument.InstrumentId}/audits");

        Assert.NotNull(identifiersResponse);
        Assert.NotNull(auditsResponse);
        Assert.Equal(5, identifiersResponse.Count);
        Assert.NotEmpty(auditsResponse);
    }

    [Fact]
    public async Task InstrumentEndpoints_ReturnNotFound_ForUnknownInstrument()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();

        var detailResponse = await client.GetAsync("/api/instruments/INS-missing");
        var identifiersResponse = await client.GetAsync("/api/instruments/INS-missing/identifiers");
        var auditsResponse = await client.GetAsync("/api/instruments/INS-missing/audits");

        Assert.Equal(HttpStatusCode.NotFound, detailResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, identifiersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, auditsResponse.StatusCode);
    }

    private sealed record InstrumentListResponse(int TotalCount, int Skip, int Take, List<InstrumentSummaryResponse> Items);
}