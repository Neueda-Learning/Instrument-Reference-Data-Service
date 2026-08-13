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

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentDetailResponse>>> Get(
        [FromQuery] string? isin,
        [FromQuery] string? cusip,
        CancellationToken cancellationToken)
    {
        var instrumentIdsQuery = dbContext.Instruments
            .AsNoTracking()
            .Select(item => item.InstrumentId);

        if (!string.IsNullOrWhiteSpace(isin))
        {
            instrumentIdsQuery = instrumentIdsQuery.Where(instrumentId => dbContext.InstrumentIdentifiers
                .Any(item => item.InstrumentId == instrumentId
                    && item.IdentifierTypeId == "ISIN"
                    && item.IdentifierValue == isin));
        }

        if (!string.IsNullOrWhiteSpace(cusip))
        {
            instrumentIdsQuery = instrumentIdsQuery.Where(instrumentId => dbContext.InstrumentIdentifiers
                .Any(item => item.InstrumentId == instrumentId
                    && item.IdentifierTypeId == "CUSIP"
                    && item.IdentifierValue == cusip));
        }

        var instrumentIds = await instrumentIdsQuery
            .OrderBy(item => item)
            .ToListAsync(cancellationToken);

        var instruments = new List<InstrumentDetailResponse>(instrumentIds.Count);
        foreach (var instrumentId in instrumentIds)
        {
            var instrument = await BuildInstrumentDetailAsync(instrumentId, cancellationToken);
            if (instrument is not null)
            {
                instruments.Add(instrument);
            }
        }

        return Ok(instruments);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var deletedCount = await dbContext.Instruments
            .Where(item => item.InstrumentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount == 0)
        {
            return NotFound();
        }

        return NoContent();
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
