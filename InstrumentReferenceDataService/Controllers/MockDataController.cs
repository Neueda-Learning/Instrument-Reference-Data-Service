using InstrumentReferenceDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/mock-data")]
public sealed class MockDataController : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromQuery] int? count,
        [FromQuery] int? seed,
        [FromServices] MockDataGenerator generator,
        CancellationToken cancellationToken)
    {
        var result = await generator.GenerateAsync(count ?? 50, seed, cancellationToken);
        return Ok(result);
    }
}
