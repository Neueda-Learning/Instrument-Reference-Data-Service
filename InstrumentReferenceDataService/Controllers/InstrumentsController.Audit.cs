using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    [HttpGet("{id}/audit")]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentAuditResponse>>> GetAuditByInstrumentId(string id, CancellationToken cancellationToken)
    {
        var instrumentExists = await dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(item => item.InstrumentId == id, cancellationToken);

        if (!instrumentExists)
        {
            return NotFound();
        }

        var audits = await dbContext.InstrumentAudits
            .AsNoTracking()
            .Where(item => item.InstrumentId == id)
            .OrderByDescending(item => item.ChangedAt)
            .SelectAuditResponse()
            .ToListAsync(cancellationToken);

        return Ok(audits);
    }
}
