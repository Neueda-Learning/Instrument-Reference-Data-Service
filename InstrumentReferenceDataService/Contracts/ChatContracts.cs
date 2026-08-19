namespace InstrumentReferenceDataService.Contracts;

public sealed record ChatMessageRequest(string Role, string Content);

public sealed record ChatRequest(IReadOnlyList<ChatMessageRequest> Messages);

public sealed record ChatResponse(string Answer);
