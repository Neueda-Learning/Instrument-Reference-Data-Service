using InstrumentReferenceDataService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInstrument(string id, [FromBody] UpdateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var instrument = await dbContext.Instruments
            .SingleOrDefaultAsync(item => item.InstrumentId == id, cancellationToken);

        if (instrument is null)
        {
            return NotFound();
        }

        var changedAt = DateTime.UtcNow;
        var hasBusinessChanges = false;

        hasBusinessChanges |= ApplyChange("name", instrument.Name, request.Name, value => instrument.Name = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("asset_class_id", instrument.AssetClassId, request.AssetClassId, value => instrument.AssetClassId = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("sector_id", instrument.SectorId.ToString(), request.SectorId.ToString(), value => instrument.SectorId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("exchange_id", instrument.ExchangeId.ToString(), request.ExchangeId.ToString(), value => instrument.ExchangeId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("currency_id", instrument.CurrencyId.ToString(), request.CurrencyId.ToString(), value => instrument.CurrencyId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("issuer_id", instrument.IssuerId.ToString(), request.IssuerId.ToString(), value => instrument.IssuerId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("status", instrument.Status, request.Status, value => instrument.Status = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("effective_date", instrument.EffectiveDate.ToString("yyyy-MM-dd"), request.EffectiveDate.ToString("yyyy-MM-dd"), value => instrument.EffectiveDate = DateOnly.Parse(value), id, changedAt);

        if (hasBusinessChanges)
        {
            instrument.LastUpdated = DateOnly.FromDateTime(changedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool ApplyChange(
        string fieldName,
        string oldValue,
        string newValue,
        Action<string> apply,
        string instrumentId,
        DateTime changedAt)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return false;
        }

        apply(newValue);

        dbContext.InstrumentAudits.Add(new InstrumentAudit
        {
            AuditId = $"AUD-{Guid.NewGuid():N}",
            InstrumentId = instrumentId,
            ChangedAt = changedAt,
            ChangedBy = "system.api",
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ChangeSource = "PUT /api/instruments/{id}"
        });

        return true;
    }
}

public sealed record UpdateInstrumentRequest(
    string Name,
    string AssetClassId,
    int SectorId,
    int ExchangeId,
    int CurrencyId,
    int IssuerId,
    string Status,
    DateOnly EffectiveDate);
