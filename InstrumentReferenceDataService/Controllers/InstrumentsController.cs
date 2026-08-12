using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InstrumentsController : ControllerBase
{
    private readonly AppDbContext dbContext;

    public InstrumentsController(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstrumentDetailResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        var instrument = await BuildInstrumentDetailAsync(id, cancellationToken);
        return instrument is null ? NotFound() : Ok(instrument);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<InstrumentDetailResponse>> LookupByIsin([FromQuery] string isin, CancellationToken cancellationToken)
    {
        var instrumentId = await dbContext.InstrumentIdentifiers
            .AsNoTracking()
            .Where(item => item.IdentifierTypeId == "ISIN" && item.IdentifierValue == isin)
            .Select(item => item.InstrumentId)
            .SingleOrDefaultAsync(cancellationToken);

        if (instrumentId is null)
        {
            return NotFound();
        }

        var instrument = await BuildInstrumentDetailAsync(instrumentId, cancellationToken);
        return instrument is null ? NotFound() : Ok(instrument);
    }

    private async Task<InstrumentDetailResponse?> BuildInstrumentDetailAsync(string instrumentId, CancellationToken cancellationToken)
    {
        var instrument = await dbContext.Instruments
            .AsNoTracking()
            .Include(item => item.AssetClass)
            .Include(item => item.Sector)
            .Include(item => item.Exchange)
            .Include(item => item.Currency)
            .Include(item => item.Issuer)
            .Where(item => item.InstrumentId == instrumentId)
            .SelectInstrumentSummary()
            .SingleOrDefaultAsync(cancellationToken);

        if (instrument is null)
        {
            return null;
        }

        var identifiers = await dbContext.InstrumentIdentifiers
            .AsNoTracking()
            .Include(item => item.IdentifierType)
            .Where(item => item.InstrumentId == instrumentId)
            .OrderBy(item => item.IdentifierTypeId)
            .SelectIdentifierResponse()
            .ToListAsync(cancellationToken);

        var audits = await dbContext.InstrumentAudits
            .AsNoTracking()
            .Where(item => item.InstrumentId == instrumentId)
            .OrderByDescending(item => item.ChangedAt)
            .SelectAuditResponse()
            .ToListAsync(cancellationToken);

        return new InstrumentDetailResponse(instrument, identifiers, audits);
    }
}
