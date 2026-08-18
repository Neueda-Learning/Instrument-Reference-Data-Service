using InstrumentReferenceDataService.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    private static readonly string[] DefaultInstrumentStatuses = ["Active", "Pending", "Suspended", "Delisted"];

    [HttpGet("options")]
    public async Task<ActionResult<InstrumentEditOptionsResponse>> GetEditOptions(CancellationToken cancellationToken)
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

        var response = new InstrumentEditOptionsResponse(
            assetClasses,
            sectors,
            exchanges,
            currencies,
            issuers,
            statusOptions);

        return Ok(response);
    }
}
