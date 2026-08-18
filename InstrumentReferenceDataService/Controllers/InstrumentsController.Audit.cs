using InstrumentReferenceDataService.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    [HttpGet("{id}/audit")]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentAuditResponse>>> GetAuditByInstrumentId(string id, CancellationToken cancellationToken)
    {
        var audits = await queryService.GetAuditByInstrumentIdAsync(id, cancellationToken);
        if (audits is null)
        {
            return NotFound();
        }

        return Ok(audits);
    }
}
