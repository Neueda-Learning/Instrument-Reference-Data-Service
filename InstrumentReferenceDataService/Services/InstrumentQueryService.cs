using System.Text.RegularExpressions;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Extensions;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Services;

public sealed class InstrumentQueryService
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

    private static readonly string[] DefaultInstrumentStatuses = ["Active", "Pending", "Suspended", "Delisted"];

    private readonly AppDbContext dbContext;

    public InstrumentQueryService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<InstrumentDetailResponse?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return BuildInstrumentDetailAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<InstrumentDetailResponse>> GetAsync(
        string? isin,
        string? cusip,
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

        return instruments;
    }

    public async Task<PagedResultResponse<InstrumentDetailResponse>> GetPagedAsync(
        string? isin,
        string? cusip,
        int pageNumber,
        int pageSize,
        string? sortBy,
        string? sortDirection,
        string? freshnessFilter,
        int staleAfterDays,
        int recentWithinDays,
        CancellationToken cancellationToken)
    {
        var normalizedPageNumber = Math.Max(1, pageNumber);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 200);
        var normalizedStaleAfterDays = Math.Max(1, staleAfterDays);
        var normalizedRecentWithinDays = Math.Max(1, recentWithinDays);

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

        return new PagedResultResponse<InstrumentDetailResponse>(
            items,
            totalCount,
            normalizedPageNumber,
            normalizedPageSize);
    }

    public async Task<MonitoringDataResponse> GetMonitoringAsync(
        int staleAfterDays,
        int recentWithinDays,
        int pageSize,
        int stalePageNumber,
        int recentPageNumber,
        int anomalyPageNumber,
        string? isin,
        string? cusip,
        CancellationToken cancellationToken)
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

        return new MonitoringDataResponse(
            freshnessScore,
            stalePaged,
            recentPaged,
            anomalyPaged);
    }

    public async Task<IReadOnlyCollection<InstrumentQualityReportItemResponse>> GetQualityReportAsync(CancellationToken cancellationToken)
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

        return instruments
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
    }

    public async Task<IReadOnlyCollection<InstrumentAuditResponse>?> GetAuditByInstrumentIdAsync(string id, CancellationToken cancellationToken)
    {
        var instrumentExists = await dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(item => item.InstrumentId == id, cancellationToken);

        if (!instrumentExists)
        {
            return null;
        }

        return await dbContext.InstrumentAudits
            .AsNoTracking()
            .Where(item => item.InstrumentId == id)
            .OrderByDescending(item => item.ChangedAt)
            .SelectAuditResponse()
            .ToListAsync(cancellationToken);
    }

    public async Task<InstrumentEditOptionsResponse> GetEditOptionsAsync(CancellationToken cancellationToken)
    {
        var assetClasses = await dbContext.AssetClasses
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new AssetClassOptionResponse(item.AssetClassId, item.Name))
            .ToListAsync(cancellationToken);

        var sectors = await dbContext.Sectors
            .AsNoTracking()
            .OrderBy(item => item.SectorName)
            .Select(item => new SectorOptionResponse(item.SectorId, item.SectorName))
            .ToListAsync(cancellationToken);

        var exchanges = await dbContext.Exchanges
            .AsNoTracking()
            .OrderBy(item => item.ExchangeName)
            .Select(item => new ExchangeOptionResponse(item.ExchangeId, item.MicCode, item.ExchangeName))
            .ToListAsync(cancellationToken);

        var currencies = await dbContext.Currencies
            .AsNoTracking()
            .OrderBy(item => item.CurrencyName)
            .Select(item => new CurrencyOptionResponse(item.CurrencyId, item.CurrencyName))
            .ToListAsync(cancellationToken);

        var issuers = await dbContext.Issuers
            .AsNoTracking()
            .OrderBy(item => item.IssuerName)
            .Select(item => new IssuerOptionResponse(item.IssuerId, item.IssuerName))
            .ToListAsync(cancellationToken);

        var statuses = await dbContext.Instruments
            .AsNoTracking()
            .Select(item => item.Status)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct()
            .ToListAsync(cancellationToken);

        var statusOptions = statuses
            .Concat(DefaultInstrumentStatuses)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item)
            .Select(item => new StatusOptionResponse(item))
            .ToList();

        return new InstrumentEditOptionsResponse(
            assetClasses,
            sectors,
            exchanges,
            currencies,
            issuers,
            statusOptions);
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
