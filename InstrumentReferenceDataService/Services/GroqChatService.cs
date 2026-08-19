using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InstrumentReferenceDataService.Contracts;

namespace InstrumentReferenceDataService.Services;

public sealed class GroqChatService
{
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    private const string Model = "openai/gpt-oss-120b";

    private const string SystemPrompt = """
        You are a specialist assistant for the Instrument Reference Data Service — a central catalogue of financial instruments used by a bank.
        
        Your role is to answer questions related to:
        - Financial instruments (equities, bonds, currencies, derivatives, futures, options)
        - Reference data concepts such as ISIN, CUSIP, SEDOL, Bloomberg ID, RIC
        - Asset classes, sectors, exchanges, currencies, issuers, and instrument identifiers
        - How reference data is used by downstream bank systems (trading, risk, compliance, reporting, settlement)
        - Data quality, source of truth, and instrument lifecycle (status, effective dates, expiry)
        - The purpose and operation of an Instrument Reference Data Service within a bank
        
        If a question is outside these topics — for example, questions about unrelated technology, personal advice, general knowledge, or anything not related to financial instruments and reference data — respond only with:
        I do not know.

        Keep the answers short and concise. DO NOT OUTPUT MARKDOWN, just plain text answers using sentences.
        
        Do not speculate or answer questions outside your defined scope, even partially.
        """;

    private readonly HttpClient httpClient;
    private readonly ILogger<GroqChatService> logger;

    public GroqChatService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqChatService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;

        var apiKey = configuration["Groq:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Groq API key is not configured. Set 'Groq:ApiKey' in user secrets.");
        }

        this.httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> GetAnswerAsync(IReadOnlyList<ChatMessageRequest> history, CancellationToken cancellationToken)
    {
        var messages = new List<GroqMessage>
        {
            new("system", SystemPrompt)
        };

        foreach (var message in history)
        {
            messages.Add(new GroqMessage(message.Role, message.Content));
        }

        var requestBody = new GroqChatRequest(Model, messages);

        var json = JsonSerializer.Serialize(requestBody, GroqSerializerContext.Default.GroqChatRequest);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        logger.LogInformation("Sending chat request to Groq API with {MessageCount} messages.", messages.Count);

        var response = await httpClient.PostAsync(GroqApiUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Groq API returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Groq API request failed with status {response.StatusCode}.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var groqResponse = JsonSerializer.Deserialize(responseJson, GroqSerializerContext.Default.GroqChatResponse);

        var answer = groqResponse?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(answer))
        {
            logger.LogWarning("Groq API returned an empty answer.");
            throw new InvalidOperationException("Groq API returned an empty response.");
        }

        logger.LogInformation("Successfully received answer from Groq API.");
        return answer;
    }
}

internal sealed record GroqMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record GroqChatRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] List<GroqMessage> Messages);

internal sealed record GroqChoiceMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record GroqChoice(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("message")] GroqChoiceMessage? Message,
    [property: JsonPropertyName("finish_reason")] string? FinishReason);

internal sealed record GroqChatResponse(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("choices")] List<GroqChoice>? Choices);

[JsonSerializable(typeof(GroqChatRequest))]
[JsonSerializable(typeof(GroqChatResponse))]
internal sealed partial class GroqSerializerContext : JsonSerializerContext;
