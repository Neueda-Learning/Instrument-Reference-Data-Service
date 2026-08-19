using System.Text.RegularExpressions;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Services;

public enum CreateInstrumentStatus
{
    Created,
    BadRequest,
    Conflict
}

public sealed record CreateInstrumentResult(
    CreateInstrumentStatus Status,
    string? ErrorMessage,
    string? CreatedInstrumentId)
{
    public static CreateInstrumentResult Created(string instrumentId) => new(CreateInstrumentStatus.Created, null, instrumentId);

    public static CreateInstrumentResult BadRequest(string message) => new(CreateInstrumentStatus.BadRequest, message, null);

    public static CreateInstrumentResult Conflict(string message) => new(CreateInstrumentStatus.Conflict, message, null);
}

public enum UpdateInstrumentStatus
{
    Updated,
    NotFound,
    BadRequest
}

public sealed record UpdateInstrumentCommand(
    string Name,
    string AssetClassId,
    int SectorId,
    int ExchangeId,
    int CurrencyId,
    int IssuerId,
    string Status,
    DateOnly EffectiveDate);

public sealed record UpdateInstrumentResult(
    UpdateInstrumentStatus Status,
    string? ErrorMessage)
{
    public static UpdateInstrumentResult Updated() => new(UpdateInstrumentStatus.Updated, null);

    public static UpdateInstrumentResult NotFound() => new(UpdateInstrumentStatus.NotFound, null);

    public static UpdateInstrumentResult BadRequest(string message) => new(UpdateInstrumentStatus.BadRequest, message);
}

public enum DeleteInstrumentStatus
{
    Deleted,
    NotFound
}

public sealed class InstrumentCommandService
{
    private static readonly Regex IsinFormatRegex = new("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled);

    private readonly AppDbContext dbContext;

    public InstrumentCommandService(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<DeleteInstrumentStatus> DeleteAsync(string? id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return DeleteInstrumentStatus.NotFound;
        }

        var deletedCount = await dbContext.Instruments
            .Where(item => item.InstrumentId == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedCount == 0
            ? DeleteInstrumentStatus.NotFound
            : DeleteInstrumentStatus.Deleted;
    }

    public async Task<CreateInstrumentResult> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InstrumentId))
        {
            return CreateInstrumentResult.BadRequest("InstrumentId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CreateInstrumentResult.BadRequest("Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.AssetClassId))
        {
            return CreateInstrumentResult.BadRequest("AssetClassId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return CreateInstrumentResult.BadRequest("Status is required");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryIsin))
        {
            return CreateInstrumentResult.BadRequest("PrimaryIsin is required");
        }

        var normalizedIsin = request.PrimaryIsin.Trim().ToUpperInvariant();
        if (!IsinFormatRegex.IsMatch(normalizedIsin))
        {
            return CreateInstrumentResult.BadRequest("PrimaryIsin must be a valid 12-character ISIN");
        }

        var existingInstrument = await dbContext.Instruments
            .AnyAsync(item => item.InstrumentId == request.InstrumentId, cancellationToken);

        if (existingInstrument)
        {
            return CreateInstrumentResult.Conflict("An instrument with this ID already exists");
        }

        var existingIsin = await dbContext.Instruments
            .AnyAsync(item => item.PrimaryIsin == normalizedIsin, cancellationToken);

        if (!existingIsin)
        {
            existingIsin = await dbContext.InstrumentIdentifiers
                .AnyAsync(item => item.IdentifierTypeId == "ISIN" && item.IdentifierValue == normalizedIsin, cancellationToken);
        }

        if (existingIsin)
        {
            return CreateInstrumentResult.Conflict("An instrument with this ISIN already exists");
        }

        var assetClassExists = await dbContext.AssetClasses
            .AnyAsync(item => item.AssetClassId == request.AssetClassId, cancellationToken);

        if (!assetClassExists)
        {
            return CreateInstrumentResult.BadRequest($"AssetClass '{request.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(item => item.SectorId == request.SectorId, cancellationToken);

        if (!sectorExists)
        {
            return CreateInstrumentResult.BadRequest($"Sector with ID {request.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(item => item.ExchangeId == request.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            return CreateInstrumentResult.BadRequest($"Exchange with ID {request.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(item => item.CurrencyId == request.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            return CreateInstrumentResult.BadRequest($"Currency with ID {request.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(item => item.IssuerId == request.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            return CreateInstrumentResult.BadRequest($"Issuer with ID {request.IssuerId} does not exist");
        }

        // Validate additional identifier types before persisting anything
        var additionalIdentifiers = request.AdditionalIdentifiers
            ?.Where(item => !string.IsNullOrWhiteSpace(item.IdentifierValue))
            .Where(item => !string.Equals(item.IdentifierTypeId, "ISIN", StringComparison.OrdinalIgnoreCase))
            .GroupBy(item => item.IdentifierTypeId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList() ?? [];

        foreach (var additionalIdentifier in additionalIdentifiers)
        {
            var typeExists = await dbContext.IdentifierTypes
                .AnyAsync(item => item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId, cancellationToken);

            if (!typeExists)
            {
                return CreateInstrumentResult.BadRequest($"Identifier type '{additionalIdentifier.IdentifierTypeId}' does not exist");
            }
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

        // Always add an ISIN identifier matching the primary ISIN
        var identifiers = new List<InstrumentIdentifier>
        {
            new()
            {
                IdentifierId = $"ID-ISIN-{request.InstrumentId}",
                InstrumentId = request.InstrumentId,
                IdentifierTypeId = "ISIN",
                IdentifierValue = normalizedIsin,
                EffectiveDate = request.EffectiveDate,
            }
        };

        identifiers.AddRange(additionalIdentifiers.Select(item => new InstrumentIdentifier
        {
            IdentifierId = $"ID-{item.IdentifierTypeId.ToUpperInvariant()}-{request.InstrumentId}",
            InstrumentId = request.InstrumentId,
            IdentifierTypeId = item.IdentifierTypeId.ToUpperInvariant(),
            IdentifierValue = item.IdentifierValue.Trim(),
            EffectiveDate = request.EffectiveDate,
        }));

        try
        {
            dbContext.Instruments.Add(instrument);
            dbContext.InstrumentIdentifiers.AddRange(identifiers);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return CreateInstrumentResult.Conflict("An instrument with this InstrumentId or PrimaryIsin already exists in the database.");
        }

        return CreateInstrumentResult.Created(request.InstrumentId);
    }

    public async Task<UpdateInstrumentResult> UpdateAsync(string id, UpdateInstrumentCommand command, CancellationToken cancellationToken)
    {
        var instrument = await dbContext.Instruments
            .SingleOrDefaultAsync(item => item.InstrumentId == id, cancellationToken);

        if (instrument is null)
        {
            return UpdateInstrumentResult.NotFound();
        }

        var assetClassExists = await dbContext.AssetClasses
            .AnyAsync(item => item.AssetClassId == command.AssetClassId, cancellationToken);

        if (!assetClassExists)
        {
            return UpdateInstrumentResult.BadRequest($"AssetClass '{command.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(item => item.SectorId == command.SectorId, cancellationToken);

        if (!sectorExists)
        {
            return UpdateInstrumentResult.BadRequest($"Sector with ID {command.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(item => item.ExchangeId == command.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            return UpdateInstrumentResult.BadRequest($"Exchange with ID {command.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(item => item.CurrencyId == command.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            return UpdateInstrumentResult.BadRequest($"Currency with ID {command.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(item => item.IssuerId == command.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            return UpdateInstrumentResult.BadRequest($"Issuer with ID {command.IssuerId} does not exist");
        }

        var changedAt = DateTime.UtcNow;
        var hasBusinessChanges = false;

        hasBusinessChanges |= ApplyChange("name", instrument.Name, command.Name, value => instrument.Name = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("asset_class_id", instrument.AssetClassId, command.AssetClassId, value => instrument.AssetClassId = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("sector_id", instrument.SectorId.ToString(), command.SectorId.ToString(), value => instrument.SectorId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("exchange_id", instrument.ExchangeId.ToString(), command.ExchangeId.ToString(), value => instrument.ExchangeId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("currency_id", instrument.CurrencyId.ToString(), command.CurrencyId.ToString(), value => instrument.CurrencyId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("issuer_id", instrument.IssuerId.ToString(), command.IssuerId.ToString(), value => instrument.IssuerId = int.Parse(value), id, changedAt);
        hasBusinessChanges |= ApplyChange("status", instrument.Status, command.Status, value => instrument.Status = value, id, changedAt);
        hasBusinessChanges |= ApplyChange(
            "effective_date",
            instrument.EffectiveDate.ToString("yyyy-MM-dd"),
            command.EffectiveDate.ToString("yyyy-MM-dd"),
            value => instrument.EffectiveDate = DateOnly.Parse(value),
            id,
            changedAt);

        if (hasBusinessChanges)
        {
            instrument.LastUpdated = DateOnly.FromDateTime(changedAt);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return UpdateInstrumentResult.Updated();
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
