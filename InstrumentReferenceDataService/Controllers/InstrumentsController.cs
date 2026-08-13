using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Extensions;
using InstrumentReferenceDataService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InstrumentsController : ControllerBase
{
    private static readonly Regex IsinFormatRegex = new("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled);
    private static readonly InstrumentQualityIndicatorResponse StatusMissingIndicator = new(
        "STATUS_MISSING",
        "Instrument status is null, empty, or whitespace.");
    private static readonly InstrumentQualityIndicatorResponse PrimaryIsinFormatInvalidIndicator = new(
        "PRIMARY_ISIN_FORMAT_INVALID",
        "Primary ISIN does not match the expected 12-character ISIN format.");
    private static readonly InstrumentQualityIndicatorResponse EffectiveDateAfterLastUpdatedIndicator = new(
        "EFFECTIVE_DATE_AFTER_LAST_UPDATED",
        "EffectiveDate is later than LastUpdated.");
    private static readonly InstrumentQualityIndicatorResponse MissingPrimaryIsinIdentifierIndicator = new(
        "PRIMARY_ISIN_IDENTIFIER_MISSING",
        "No active ISIN identifier exists that matches the instrument PrimaryIsin.");
    private static readonly InstrumentQualityIndicatorResponse IdentifierDateRangeInvalidIndicator = new(
        "IDENTIFIER_DATE_RANGE_INVALID",
        "At least one identifier has ExpiryDate earlier than EffectiveDate.");
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

    [HttpGet("quality-report")]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentQualityReportItemResponse>>> GetQualityReport(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var instruments = await dbContext.Instruments
            .AsNoTracking()
            .Select(item => new
            {
                item.InstrumentId,
                item.Name,
                item.PrimaryIsin,
                item.Status,
                item.EffectiveDate,
                item.LastUpdated,
                HasMatchingPrimaryIsinIdentifier = item.Identifiers.Any(identifier =>
                    identifier.IdentifierTypeId == "ISIN"
                    && identifier.IdentifierValue == item.PrimaryIsin
                    && identifier.EffectiveDate <= today
                    && (identifier.ExpiryDate == null || identifier.ExpiryDate >= today)),
                HasInvalidIdentifierDateRange = item.Identifiers.Any(identifier =>
                    identifier.ExpiryDate != null && identifier.ExpiryDate < identifier.EffectiveDate)
            })
            .ToListAsync(cancellationToken);

        var reportItems = instruments
            .Select(item =>
            {
                var indicators = new List<InstrumentQualityIndicatorResponse>();

                if (string.IsNullOrWhiteSpace(item.Status))
                {
                    indicators.Add(StatusMissingIndicator);
                }

                if (!IsinFormatRegex.IsMatch(item.PrimaryIsin ?? string.Empty))
                {
                    indicators.Add(PrimaryIsinFormatInvalidIndicator);
                }

                if (item.EffectiveDate > item.LastUpdated)
                {
                    indicators.Add(EffectiveDateAfterLastUpdatedIndicator);
                }

                if (!item.HasMatchingPrimaryIsinIdentifier)
                {
                    indicators.Add(MissingPrimaryIsinIdentifierIndicator);
                }

                if (item.HasInvalidIdentifierDateRange)
                {
                    indicators.Add(IdentifierDateRangeInvalidIndicator);
                }

                return new InstrumentQualityReportItemResponse(
                    item.InstrumentId,
                    item.Name,
                    item.PrimaryIsin,
                    indicators);
            })
            .Where(item => item.FailingIndicators.Count > 0)
            .OrderBy(item => item.InstrumentId)
            .ToList();

        return Ok(reportItems);
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentDetailResponse>> Create([FromBody] CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InstrumentId))
        {
            return BadRequest("InstrumentId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.AssetClassId))
        {
            return BadRequest("AssetClassId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest("Status is required");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryIsin))
        {
            return BadRequest("PrimaryIsin is required");
        }

        var normalizedIsin = request.PrimaryIsin.Trim().ToUpperInvariant();
        if (!IsinFormatRegex.IsMatch(normalizedIsin))
        {
            return BadRequest("PrimaryIsin must be a valid 12-character ISIN");
        }

        // Check if instrument ID already exists
        var existingInstrument = await dbContext.Instruments
            .AnyAsync(i => i.InstrumentId == request.InstrumentId, cancellationToken);

        if (existingInstrument)
        {
            return Conflict("An instrument with this ID already exists");
        }

        // Check if ISIN already exists
        var existingIsin = await dbContext.Instruments
            .AnyAsync(i => i.PrimaryIsin == normalizedIsin, cancellationToken);

        if (!existingIsin)
        {
            existingIsin = await dbContext.InstrumentIdentifiers
                .AnyAsync(i => i.IdentifierTypeId == "ISIN" && i.IdentifierValue == normalizedIsin, cancellationToken);
        }

        if (existingIsin)
        {
            return Conflict("An instrument with this ISIN already exists");
        }

        // Verify that all required foreign keys exist
        var assetClassExists = await dbContext.AssetClasses
            .AnyAsync(ac => ac.AssetClassId == request.AssetClassId, cancellationToken);

        if (!assetClassExists)
        {
            return BadRequest($"AssetClass '{request.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(s => s.SectorId == request.SectorId, cancellationToken);

        if (!sectorExists)
        {
            return BadRequest($"Sector with ID {request.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(e => e.ExchangeId == request.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            return BadRequest($"Exchange with ID {request.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(c => c.CurrencyId == request.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            return BadRequest($"Currency with ID {request.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(i => i.IssuerId == request.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            return BadRequest($"Issuer with ID {request.IssuerId} does not exist");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var instrument = new Instrument
        {
            InstrumentId = request.InstrumentId,
            Name = request.Name,
            PrimaryIsin = normalizedIsin,
            AssetClassId = request.AssetClassId,
            SectorId = request.SectorId,
            ExchangeId = request.ExchangeId,
            CurrencyId = request.CurrencyId,
            IssuerId = request.IssuerId,
            Status = request.Status,
            EffectiveDate = request.EffectiveDate,
            LastUpdated = today
        };

        try
        {
            dbContext.Instruments.Add(instrument);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
           
            return Conflict("An instrument with this InstrumentId or PrimaryIsin already exists in the database.");
        }

        var result = await BuildInstrumentDetailAsync(request.InstrumentId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = request.InstrumentId }, result);
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
