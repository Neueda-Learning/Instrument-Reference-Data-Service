using InstrumentReferenceDataService.Contracts;
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
            request.PrimaryIsin,
            request.AssetClassId,
            request.SectorId,
            request.ExchangeId,
            request.CurrencyId,
            request.IssuerId,
            request.Status,
            request.EffectiveDate,
            request.AdditionalIdentifiers);

        var result = await commandService.UpdateAsync(id, command, cancellationToken);
        if (result.Status == UpdateInstrumentStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == UpdateInstrumentStatus.BadRequest)
        {
            if (result.ValidationErrors is { Count: > 0 })
            {
                var validationProblem = new ValidationProblemDetails
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                };

                foreach (var error in result.ValidationErrors)
                {
                    validationProblem.Errors.Add(error.Key, error.Value);
                }

                return BadRequest(validationProblem);
            }

            return BadRequest(result.ErrorMessage);
        }

        if (result.Status == UpdateInstrumentStatus.Conflict)
        {
            return Conflict(result.ErrorMessage);
        }

        return NoContent();
    }
}

public sealed record UpdateInstrumentRequest(
    string Name,
    string PrimaryIsin,
    string AssetClassId,
    int SectorId,
    int ExchangeId,
    int CurrencyId,
    int IssuerId,
    string Status,
    DateOnly EffectiveDate,
    IReadOnlyCollection<AdditionalIdentifierInput>? AdditionalIdentifiers = null);
