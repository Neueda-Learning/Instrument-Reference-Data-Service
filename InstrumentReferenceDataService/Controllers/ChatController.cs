using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly GroqChatService groqChatService;
    private readonly ILogger<ChatController> logger;

    public ChatController(GroqChatService groqChatService, ILogger<ChatController> logger)
    {
        this.groqChatService = groqChatService;
        this.logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        if (request.Messages is null || request.Messages.Count == 0)
        {
            return BadRequest("At least one message is required.");
        }

        logger.LogInformation("Received chat request with {MessageCount} messages.", request.Messages.Count);

        var answer = await groqChatService.GetAnswerAsync(request.Messages, cancellationToken);

        return Ok(new ChatResponse(answer));
    }
}
