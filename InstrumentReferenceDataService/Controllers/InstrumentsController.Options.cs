using InstrumentReferenceDataService.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    [HttpGet("options")]
    public async Task<ActionResult<InstrumentEditOptionsResponse>> GetEditOptions(CancellationToken cancellationToken)
    {
        var response = await queryService.GetEditOptionsAsync(cancellationToken);

        return Ok(response);
    }

}
