using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Extensions;
using InstrumentReferenceDataService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed partial class InstrumentsController : ControllerBase
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
    private readonly ILogger<InstrumentsController> logger;

    public InstrumentsController(AppDbContext dbContext, ILogger<InstrumentsController> logger)
    {
        this.dbContext = dbContext;
        this.logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstrumentDetailResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to retrieve instrument by ID: {InstrumentId}", id);
        var instrument = await BuildInstrumentDetailAsync(id, cancellationToken);
        if (instrument is null)
        {
            logger.LogWarning("Instrument with ID: {InstrumentId} not found", id);
            return NotFound();
        }
        
        logger.LogInformation("Successfully retrieved instrument with ID: {InstrumentId}", id);
        return Ok(instrument);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentDetailResponse>>> Get(
        [FromQuery] string? isin,
        [FromQuery] string? cusip,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying for instruments with ISIN: {ISIN} and CUSIP: {CUSIP}", isin, cusip);
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
        
        logger.LogInformation("Found {Count} instruments matching query.", instruments.Count);
        return Ok(instruments);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultResponse<InstrumentDetailResponse>>> GetPaged(
        [FromQuery] string? isin,
        [FromQuery] string? cusip,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? sortBy = "instrumentId",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? freshnessFilter = null,
        [FromQuery] int staleAfterDays = 30,
        [FromQuery] int recentWithinDays = 7,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var normalizedStaleAfterDays = Math.Max(1, staleAfterDays);
        var normalizedRecentWithinDays = Math.Max(1, recentWithinDays);

        logger.LogInformation(
            "Paged query for instruments with ISIN {ISIN}, CUSIP {CUSIP}, page {PageNumber}, size {PageSize}, sort {SortBy} {SortDirection}, freshness {FreshnessFilter}",
            isin,
            cusip,
            normalizedPageNumber,
            normalizedPageSize,
            sortBy,
            sortDirection,
            freshnessFilter);

        var query = ApplyIdentifierFilters(dbContext.Instruments.AsNoTracking(), isin, cusip);

        var normalizedSortBy = (sortBy ?? "instrumentId").Trim().ToLowerInvariant();
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        IOrderedQueryable<Instrument> orderedQuery;
        orderedQuery = normalizedSortBy switch
        {
            "name" => descending ? query.OrderByDescending(item => item.Name).ThenBy(item => item.InstrumentId) : query.OrderBy(item => item.Name).ThenBy(item => item.InstrumentId),
            "lastupdated" => descending ? query.OrderByDescending(item => item.LastUpdated).ThenBy(item => item.InstrumentId) : query.OrderBy(item => item.LastUpdated).ThenBy(item => item.InstrumentId),
            _ => descending ? query.OrderByDescending(item => item.InstrumentId) : query.OrderBy(item => item.InstrumentId)
        };

        List<string> instrumentIds;
        int totalCount;

        if (!string.IsNullOrWhiteSpace(freshnessFilter))
        {
            var normalizedFilter = freshnessFilter.Trim().ToLowerInvariant();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var sortedCandidates = await orderedQuery
                .Select(item => new
                {
                    item.InstrumentId,
                    item.LastUpdated
                })
                .ToListAsync(cancellationToken);

            var filteredCandidates = sortedCandidates
                .Where(item =>
                {
                    var ageDays = today.DayNumber - item.LastUpdated.DayNumber;
                    return normalizedFilter switch
                    {
                        "stale" => ageDays > normalizedStaleAfterDays,
                        "recent" => ageDays >= 0 && ageDays <= normalizedRecentWithinDays,
                        _ => true
                    };
                })
                .ToList();

            totalCount = filteredCandidates.Count;
            instrumentIds = filteredCandidates
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(item => item.InstrumentId)
                .ToList();
        }
        else
        {
            totalCount = await orderedQuery.CountAsync(cancellationToken);
            instrumentIds = await orderedQuery
                .Skip((normalizedPageNumber - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(item => item.InstrumentId)
                .ToListAsync(cancellationToken);
        }

        var items = new List<InstrumentDetailResponse>(instrumentIds.Count);
        foreach (var instrumentId in instrumentIds)
        {
            var instrument = await BuildInstrumentDetailAsync(instrumentId, cancellationToken);
            if (instrument is not null)
            {
                items.Add(instrument);
            }
        }

        var response = new PagedResultResponse<InstrumentDetailResponse>(
            items,
            totalCount,
            normalizedPageNumber,
            normalizedPageSize);

        return Ok(response);
    }

    [HttpGet("monitoring")]
    public async Task<ActionResult<MonitoringDataResponse>> GetMonitoring(
        [FromQuery] int staleAfterDays = 30,
        [FromQuery] int recentWithinDays = 7,
        [FromQuery] int pageSize = 8,
        [FromQuery] int stalePageNumber = 1,
        [FromQuery] int recentPageNumber = 1,
        [FromQuery] int anomalyPageNumber = 1,
        [FromQuery] string? isin = null,
        [FromQuery] string? cusip = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedStaleAfterDays = Math.Max(1, staleAfterDays);
        var normalizedRecentWithinDays = Math.Max(1, recentWithinDays);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var normalizedStalePage = Math.Max(1, stalePageNumber);
        var normalizedRecentPage = Math.Max(1, recentPageNumber);
        var normalizedAnomalyPage = Math.Max(1, anomalyPageNumber);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = ApplyIdentifierFilters(dbContext.Instruments.AsNoTracking(), isin, cusip);

        var sourceItems = await query
            .Select(item => new
            {
                item.InstrumentId,
                item.Name,
                item.LastUpdated
            })
            .ToListAsync(cancellationToken);

        var staleItems = new List<MonitoringInstrumentItemResponse>();
        var recentItems = new List<MonitoringInstrumentItemResponse>();
        var anomalyItems = new List<MonitoringAnomalyItemResponse>();

        foreach (var item in sourceItems)
        {
            var ageDays = today.DayNumber - item.LastUpdated.DayNumber;

            if (ageDays < 0)
            {
                anomalyItems.Add(new MonitoringAnomalyItemResponse(
                    item.InstrumentId,
                    item.Name,
                    item.LastUpdated,
                    "Last Updated is in the future"));
            }

            if (ageDays > normalizedStaleAfterDays)
            {
                staleItems.Add(new MonitoringInstrumentItemResponse(
                    item.InstrumentId,
                    item.Name,
                    item.LastUpdated,
                    ageDays));
            }

            if (ageDays >= 0 && ageDays <= normalizedRecentWithinDays)
            {
                recentItems.Add(new MonitoringInstrumentItemResponse(
                    item.InstrumentId,
                    item.Name,
                    item.LastUpdated,
                    ageDays));
            }
        }

        staleItems = staleItems
            .OrderByDescending(item => item.AgeDays)
            .ThenBy(item => item.InstrumentId)
            .ToList();

        recentItems = recentItems
            .OrderBy(item => item.AgeDays)
            .ThenBy(item => item.InstrumentId)
            .ToList();

        anomalyItems = anomalyItems
            .OrderBy(item => item.InstrumentId)
            .ToList();

        var monitoredCount = staleItems.Count + recentItems.Count;
        var freshnessScore = monitoredCount == 0
            ? 100
            : Math.Max(0, (int)Math.Round((double)(recentItems.Count * 100) / monitoredCount, MidpointRounding.AwayFromZero));

        var stalePaged = BuildPagedResult(staleItems, normalizedStalePage, normalizedPageSize);
        var recentPaged = BuildPagedResult(recentItems, normalizedRecentPage, normalizedPageSize);
        var anomalyPaged = BuildPagedResult(anomalyItems, normalizedAnomalyPage, normalizedPageSize);

        var response = new MonitoringDataResponse(
            freshnessScore,
            stalePaged,
            recentPaged,
            anomalyPaged);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string? id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete instrument with ID: {InstrumentId}", id);
        if (string.IsNullOrWhiteSpace(id))
        {
            logger.LogWarning("Delete failed: Instrument ID was null or whitespace.");
            return NotFound();
        }

        var deletedCount = await dbContext.Instruments
            .Where(item => item.InstrumentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedCount == 0)
        {
            logger.LogWarning("Delete failed: Instrument with ID: {InstrumentId} not found.", id);
            return NotFound();
        }

        logger.LogInformation("Successfully deleted instrument with ID: {InstrumentId}", id);
        return NoContent();
    }

    [HttpGet("quality-report")]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentQualityReportItemResponse>>> GetQualityReport(CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating data quality report.");
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
        
        logger.LogInformation("Data quality report generated. Found {Count} instruments with issues.", reportItems.Count);
        return Ok(reportItems);
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentDetailResponse>> Create([FromBody] CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to create instrument with ID: {InstrumentId}", request.InstrumentId);
        
        if (string.IsNullOrWhiteSpace(request.InstrumentId))
        {
            logger.LogWarning("Validation failed for new instrument: InstrumentId is required.");
            return BadRequest("InstrumentId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Name is required.", request.InstrumentId);
            return BadRequest("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.AssetClassId))
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: AssetClassId is required.", request.InstrumentId);
            return BadRequest("AssetClassId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Status is required.", request.InstrumentId);
            return BadRequest("Status is required");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryIsin))
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: PrimaryIsin is required.", request.InstrumentId);
            return BadRequest("PrimaryIsin is required");
        }

        var normalizedIsin = request.PrimaryIsin.Trim().ToUpperInvariant();
        if (!IsinFormatRegex.IsMatch(normalizedIsin))
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: PrimaryIsin '{PrimaryIsin}' has an invalid format.", request.InstrumentId, request.PrimaryIsin);
            return BadRequest("PrimaryIsin must be a valid 12-character ISIN");
        }

        // Check if instrument ID already exists
        var existingInstrument = await dbContext.Instruments
            .AnyAsync(i => i.InstrumentId == request.InstrumentId, cancellationToken);

        if (existingInstrument)
        {
            logger.LogWarning("Conflict: An instrument with ID {InstrumentId} already exists.", request.InstrumentId);
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
            logger.LogWarning("Conflict: An instrument with ISIN {PrimaryIsin} already exists.", normalizedIsin);
            return Conflict("An instrument with this ISIN already exists");
        }

        // Verify that all required foreign keys exist
        var assetClassExists = await dbContext.AssetClasses
            .AnyAsync(ac => ac.AssetClassId == request.AssetClassId, cancellationToken);

        if (!assetClassExists)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: AssetClass '{AssetClassId}' does not exist.", request.InstrumentId, request.AssetClassId);
            return BadRequest($"AssetClass '{request.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(s => s.SectorId == request.SectorId, cancellationToken);

        if (!sectorExists)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Sector '{SectorId}' does not exist.", request.InstrumentId, request.SectorId);
            return BadRequest($"Sector with ID {request.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(e => e.ExchangeId == request.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Exchange '{ExchangeId}' does not exist.", request.InstrumentId, request.ExchangeId);
            return BadRequest($"Exchange with ID {request.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(c => c.CurrencyId == request.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Currency '{CurrencyId}' does not exist.", request.InstrumentId, request.CurrencyId);
            return BadRequest($"Currency with ID {request.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(i => i.IssuerId == request.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: Issuer '{IssuerId}' does not exist.", request.InstrumentId, request.IssuerId);
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
            logger.LogInformation("Successfully created instrument with ID {InstrumentId} and ISIN {PrimaryIsin}", instrument.InstrumentId, instrument.PrimaryIsin);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database error occurred while creating instrument {InstrumentId}.", request.InstrumentId);
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

    private IQueryable<Instrument> ApplyIdentifierFilters(IQueryable<Instrument> query, string? isin, string? cusip)
    {
        if (!string.IsNullOrWhiteSpace(isin))
        {
            var normalizedIsin = isin.Trim().ToUpperInvariant();
            query = query.Where(instrument => dbContext.InstrumentIdentifiers
                .Any(identifier => identifier.InstrumentId == instrument.InstrumentId
                    && identifier.IdentifierTypeId == "ISIN"
                    && identifier.IdentifierValue == normalizedIsin));
        }

        if (!string.IsNullOrWhiteSpace(cusip))
        {
            var normalizedCusip = cusip.Trim().ToUpperInvariant();
            query = query.Where(instrument => dbContext.InstrumentIdentifiers
                .Any(identifier => identifier.InstrumentId == instrument.InstrumentId
                    && identifier.IdentifierTypeId == "CUSIP"
                    && identifier.IdentifierValue == normalizedCusip));
        }

        return query;
    }

    private static PagedResultResponse<T> BuildPagedResult<T>(IReadOnlyList<T> source, int pageNumber, int pageSize)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var totalCount = source.Count;

        var items = source
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToList();

        return new PagedResultResponse<T>(items, totalCount, normalizedPageNumber, normalizedPageSize);
    }
}
