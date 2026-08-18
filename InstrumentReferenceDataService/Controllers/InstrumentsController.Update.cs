using InstrumentReferenceDataService.Services;
using Microsoft.AspNetCore.Mvc;

namespace InstrumentReferenceDataService.Controllers;

public sealed partial class InstrumentsController
{
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInstrument(string id, [FromBody] UpdateInstrumentRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateInstrumentCommand(
            request.Name,
            request.AssetClassId,
            request.SectorId,
            request.ExchangeId,
            request.CurrencyId,
            request.IssuerId,
            request.Status,
            request.EffectiveDate);

        var result = await commandService.UpdateAsync(id, command, cancellationToken);
        if (result.Status == UpdateInstrumentStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == UpdateInstrumentStatus.BadRequest)
        {
            return BadRequest(result.ErrorMessage);
        }

        return NoContent();
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
