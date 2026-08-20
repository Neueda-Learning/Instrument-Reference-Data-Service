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
    string? CreatedInstrumentId,
    IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static CreateInstrumentResult Created(string instrumentId) => new(CreateInstrumentStatus.Created, null, instrumentId, null);

    public static CreateInstrumentResult BadRequest(string field, string message) =>
        new(
            CreateInstrumentStatus.BadRequest,
            message,
            null,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = [message]
            });

    public static CreateInstrumentResult BadRequest(IDictionary<string, string[]> validationErrors) =>
        new(
            CreateInstrumentStatus.BadRequest,
            "One or more validation errors occurred.",
            null,
            new Dictionary<string, string[]>(validationErrors, StringComparer.Ordinal));

    public static CreateInstrumentResult Conflict(string message) => new(CreateInstrumentStatus.Conflict, message, null, null);
}

public enum UpdateInstrumentStatus
{
    Updated,
    NotFound,
    BadRequest,
    Conflict
}

public sealed record UpdateInstrumentCommand(
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

public sealed record UpdateInstrumentResult(
    UpdateInstrumentStatus Status,
    string? ErrorMessage,
    IReadOnlyDictionary<string, string[]>? ValidationErrors)
{
    public static UpdateInstrumentResult Updated() => new(UpdateInstrumentStatus.Updated, null, null);

    public static UpdateInstrumentResult NotFound() => new(UpdateInstrumentStatus.NotFound, null, null);

    public static UpdateInstrumentResult BadRequest(string field, string message) =>
        new(
            UpdateInstrumentStatus.BadRequest,
            message,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = [message]
            });

    public static UpdateInstrumentResult BadRequest(IDictionary<string, string[]> validationErrors) =>
        new(
            UpdateInstrumentStatus.BadRequest,
            "One or more validation errors occurred.",
            new Dictionary<string, string[]>(validationErrors, StringComparer.Ordinal));

    public static UpdateInstrumentResult Conflict(string message) =>
        new(UpdateInstrumentStatus.Conflict, message, null);
}

public enum DeleteInstrumentStatus
{
    Deleted,
    NotFound
}

public sealed class InstrumentCommandService
{
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
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.InstrumentId), "InstrumentId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.Name), "Name is required");
        }

        if (string.IsNullOrWhiteSpace(request.AssetClassId))
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.AssetClassId), "AssetClassId is required");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.Status), "Status is required");
        }

        if (string.IsNullOrWhiteSpace(request.PrimaryIsin))
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.PrimaryIsin), "PrimaryIsin is required");
        }

        if (!IdentifierFormatValidator.TryNormalizeAndValidate("ISIN", request.PrimaryIsin, out var normalizedIsin, out var primaryIsinValidationError))
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.PrimaryIsin), primaryIsinValidationError!);
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
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.AssetClassId), $"AssetClass '{request.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(item => item.SectorId == request.SectorId, cancellationToken);

        if (!sectorExists)
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.SectorId), $"Sector with ID {request.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(item => item.ExchangeId == request.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.ExchangeId), $"Exchange with ID {request.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(item => item.CurrencyId == request.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.CurrencyId), $"Currency with ID {request.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(item => item.IssuerId == request.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            return CreateInstrumentResult.BadRequest(nameof(CreateInstrumentRequest.IssuerId), $"Issuer with ID {request.IssuerId} does not exist");
        }

        // Validate additional identifier types before persisting anything
        var additionalIdentifiers = request.AdditionalIdentifiers
            ?.Where(item => !string.IsNullOrWhiteSpace(item.IdentifierValue))
            .Where(item => !string.IsNullOrWhiteSpace(item.IdentifierTypeId))
            .Where(item => !string.Equals(item.IdentifierTypeId, "ISIN", StringComparison.OrdinalIgnoreCase))
            .Select(item => new
            {
                IdentifierTypeId = item.IdentifierTypeId.Trim().ToUpperInvariant(),
                IdentifierValue = item.IdentifierValue.Trim()
            })
            .GroupBy(item => item.IdentifierTypeId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList() ?? [];

        var validatedAdditionalIdentifiers = new List<(string IdentifierTypeId, string IdentifierValue)>();

        foreach (var additionalIdentifier in additionalIdentifiers)
        {
            var typeExists = await dbContext.IdentifierTypes
                .AnyAsync(item => item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId, cancellationToken);

            if (!typeExists)
            {
                return CreateInstrumentResult.BadRequest(
                    $"{nameof(CreateInstrumentRequest.AdditionalIdentifiers)}[{additionalIdentifier.IdentifierTypeId}].{nameof(AdditionalIdentifierInput.IdentifierTypeId)}",
                    $"Identifier type '{additionalIdentifier.IdentifierTypeId}' does not exist");
            }

            if (!IdentifierFormatValidator.TryNormalizeAndValidate(
                    additionalIdentifier.IdentifierTypeId,
                    additionalIdentifier.IdentifierValue,
                    out var normalizedIdentifierValue,
                    out var additionalIdentifierValidationError))
            {
                return CreateInstrumentResult.BadRequest(
                    $"{nameof(CreateInstrumentRequest.AdditionalIdentifiers)}[{additionalIdentifier.IdentifierTypeId}].{nameof(AdditionalIdentifierInput.IdentifierValue)}",
                    additionalIdentifierValidationError!);
            }

            validatedAdditionalIdentifiers.Add((additionalIdentifier.IdentifierTypeId, normalizedIdentifierValue));
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

        identifiers.AddRange(validatedAdditionalIdentifiers.Select(item => new InstrumentIdentifier
        {
            IdentifierId = $"ID-{item.IdentifierTypeId}-{request.InstrumentId}",
            InstrumentId = request.InstrumentId,
            IdentifierTypeId = item.IdentifierTypeId,
            IdentifierValue = item.IdentifierValue,
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
            var conflictDetails = await BuildCreateConflictDetailsAsync(
                request.InstrumentId,
                normalizedIsin,
                validatedAdditionalIdentifiers,
                cancellationToken);

            return CreateInstrumentResult.Conflict(conflictDetails);
        }

        return CreateInstrumentResult.Created(request.InstrumentId);
    }

    private async Task<string> BuildCreateConflictDetailsAsync(
        string instrumentId,
        string normalizedIsin,
        IReadOnlyCollection<(string IdentifierTypeId, string IdentifierValue)> additionalIdentifiers,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<string>();

        var instrumentIdExists = await dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(item => item.InstrumentId == instrumentId, cancellationToken);
        if (instrumentIdExists)
        {
            conflicts.Add($"InstrumentId '{instrumentId}'");
        }

        var primaryIsinExists = await dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(item => item.PrimaryIsin == normalizedIsin, cancellationToken);
        if (primaryIsinExists)
        {
            conflicts.Add($"PrimaryIsin '{normalizedIsin}'");
        }

        foreach (var additionalIdentifier in additionalIdentifiers)
        {
            var exists = await dbContext.InstrumentIdentifiers
                .AsNoTracking()
                .AnyAsync(
                    item => item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId
                        && item.IdentifierValue == additionalIdentifier.IdentifierValue,
                    cancellationToken);

            if (exists)
            {
                conflicts.Add(
                    $"{additionalIdentifier.IdentifierTypeId} ({additionalIdentifier.IdentifierValue})");
            }
        }

        if (conflicts.Count == 0)
        {
            return "A unique constraint was violated while creating the instrument.";
        }

        return $"Duplicate value(s) detected for: {string.Join(", ", conflicts)}.";
    }

    public async Task<UpdateInstrumentResult> UpdateAsync(string id, UpdateInstrumentCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.Name), "Name is required");
        }

        if (string.IsNullOrWhiteSpace(command.PrimaryIsin))
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.PrimaryIsin), "PrimaryIsin is required");
        }

        if (string.IsNullOrWhiteSpace(command.AssetClassId))
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.AssetClassId), "AssetClassId is required");
        }

        if (string.IsNullOrWhiteSpace(command.Status))
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.Status), "Status is required");
        }

        if (!IdentifierFormatValidator.TryNormalizeAndValidate("ISIN", command.PrimaryIsin, out var normalizedIsin, out var primaryIsinValidationError))
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.PrimaryIsin), primaryIsinValidationError!);
        }

        var instrument = await dbContext.Instruments
            .SingleOrDefaultAsync(item => item.InstrumentId == id, cancellationToken);

        if (instrument is null)
        {
            return UpdateInstrumentResult.NotFound();
        }

        var conflictingPrimaryIsin = await dbContext.Instruments
            .AnyAsync(item => item.InstrumentId != id && item.PrimaryIsin == normalizedIsin, cancellationToken);

        if (!conflictingPrimaryIsin)
        {
            conflictingPrimaryIsin = await dbContext.InstrumentIdentifiers
                .AnyAsync(
                    item => item.InstrumentId != id
                        && item.IdentifierTypeId == "ISIN"
                        && item.IdentifierValue == normalizedIsin,
                    cancellationToken);
        }

        if (conflictingPrimaryIsin)
        {
            return UpdateInstrumentResult.Conflict("An instrument with this ISIN already exists");
        }

        var assetClassExists = await dbContext.AssetClasses
            .AnyAsync(item => item.AssetClassId == command.AssetClassId, cancellationToken);

        if (!assetClassExists)
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.AssetClassId), $"AssetClass '{command.AssetClassId}' does not exist");
        }

        var sectorExists = await dbContext.Sectors
            .AnyAsync(item => item.SectorId == command.SectorId, cancellationToken);

        if (!sectorExists)
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.SectorId), $"Sector with ID {command.SectorId} does not exist");
        }

        var exchangeExists = await dbContext.Exchanges
            .AnyAsync(item => item.ExchangeId == command.ExchangeId, cancellationToken);

        if (!exchangeExists)
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.ExchangeId), $"Exchange with ID {command.ExchangeId} does not exist");
        }

        var currencyExists = await dbContext.Currencies
            .AnyAsync(item => item.CurrencyId == command.CurrencyId, cancellationToken);

        if (!currencyExists)
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.CurrencyId), $"Currency with ID {command.CurrencyId} does not exist");
        }

        var issuerExists = await dbContext.Issuers
            .AnyAsync(item => item.IssuerId == command.IssuerId, cancellationToken);

        if (!issuerExists)
        {
            return UpdateInstrumentResult.BadRequest(nameof(UpdateInstrumentCommand.IssuerId), $"Issuer with ID {command.IssuerId} does not exist");
        }

        var additionalIdentifiers = command.AdditionalIdentifiers
            ?.Where(item => !string.IsNullOrWhiteSpace(item.IdentifierValue))
            .Where(item => !string.IsNullOrWhiteSpace(item.IdentifierTypeId))
            .Where(item => !string.Equals(item.IdentifierTypeId, "ISIN", StringComparison.OrdinalIgnoreCase))
            .Select(item => new
            {
                IdentifierTypeId = item.IdentifierTypeId.Trim().ToUpperInvariant(),
                IdentifierValue = item.IdentifierValue.Trim()
            })
            .GroupBy(item => item.IdentifierTypeId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList() ?? [];

        var validatedAdditionalIdentifiers = new List<(string IdentifierTypeId, string IdentifierValue)>();

        foreach (var additionalIdentifier in additionalIdentifiers)
        {
            var typeExists = await dbContext.IdentifierTypes
                .AnyAsync(item => item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId, cancellationToken);

            if (!typeExists)
            {
                return UpdateInstrumentResult.BadRequest(
                    $"{nameof(UpdateInstrumentCommand.AdditionalIdentifiers)}[{additionalIdentifier.IdentifierTypeId}].{nameof(AdditionalIdentifierInput.IdentifierTypeId)}",
                    $"Identifier type '{additionalIdentifier.IdentifierTypeId}' does not exist");
            }

            if (!IdentifierFormatValidator.TryNormalizeAndValidate(
                    additionalIdentifier.IdentifierTypeId,
                    additionalIdentifier.IdentifierValue,
                    out var normalizedIdentifierValue,
                    out var additionalIdentifierValidationError))
            {
                return UpdateInstrumentResult.BadRequest(
                    $"{nameof(UpdateInstrumentCommand.AdditionalIdentifiers)}[{additionalIdentifier.IdentifierTypeId}].{nameof(AdditionalIdentifierInput.IdentifierValue)}",
                    additionalIdentifierValidationError!);
            }

            validatedAdditionalIdentifiers.Add((additionalIdentifier.IdentifierTypeId, normalizedIdentifierValue));
        }

        var conflictingIdentifiers = new List<string>();
        foreach (var additionalIdentifier in validatedAdditionalIdentifiers)
        {
            var exists = await dbContext.InstrumentIdentifiers
                .AnyAsync(
                    item => item.InstrumentId != id
                        && item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId
                        && item.IdentifierValue == additionalIdentifier.IdentifierValue,
                    cancellationToken);

            if (exists)
            {
                conflictingIdentifiers.Add($"{additionalIdentifier.IdentifierTypeId} ({additionalIdentifier.IdentifierValue})");
            }
        }

        if (conflictingIdentifiers.Count > 0)
        {
            return UpdateInstrumentResult.Conflict($"Duplicate value(s) detected for: {string.Join(", ", conflictingIdentifiers)}.");
        }

        var changedAt = DateTime.UtcNow;
        var hasBusinessChanges = false;

        hasBusinessChanges |= ApplyChange("name", instrument.Name, command.Name, value => instrument.Name = value, id, changedAt);
        hasBusinessChanges |= ApplyChange("primary_isin", instrument.PrimaryIsin, normalizedIsin, value => instrument.PrimaryIsin = value, id, changedAt);
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

        var desiredIdentifierValues = validatedAdditionalIdentifiers
            .ToDictionary(item => item.IdentifierTypeId, item => item.IdentifierValue, StringComparer.OrdinalIgnoreCase);
        desiredIdentifierValues["ISIN"] = normalizedIsin;

        var existingIdentifiers = await dbContext.InstrumentIdentifiers
            .Where(item => item.InstrumentId == id)
            .ToListAsync(cancellationToken);

        foreach (var existingIdentifier in existingIdentifiers)
        {
            if (desiredIdentifierValues.TryGetValue(existingIdentifier.IdentifierTypeId, out var desiredValue))
            {
                if (!string.Equals(existingIdentifier.IdentifierValue, desiredValue, StringComparison.Ordinal))
                {
                    AddAuditEntry(
                        $"identifier_{existingIdentifier.IdentifierTypeId}",
                        existingIdentifier.IdentifierValue,
                        desiredValue,
                        id,
                        changedAt);

                    existingIdentifier.IdentifierValue = desiredValue;
                    existingIdentifier.EffectiveDate = command.EffectiveDate;
                    existingIdentifier.ExpiryDate = null;
                    hasBusinessChanges = true;
                }

                desiredIdentifierValues.Remove(existingIdentifier.IdentifierTypeId);
                continue;
            }

            if (string.Equals(existingIdentifier.IdentifierTypeId, "ISIN", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AddAuditEntry(
                $"identifier_{existingIdentifier.IdentifierTypeId}",
                existingIdentifier.IdentifierValue,
                null,
                id,
                changedAt);

            dbContext.InstrumentIdentifiers.Remove(existingIdentifier);
            hasBusinessChanges = true;
        }

        foreach (var missingIdentifier in desiredIdentifierValues)
        {
            var identifierTypeId = missingIdentifier.Key.ToUpperInvariant();
            var identifierValue = missingIdentifier.Value;

            dbContext.InstrumentIdentifiers.Add(new InstrumentIdentifier
            {
                IdentifierId = $"ID-{identifierTypeId}-{id}",
                InstrumentId = id,
                IdentifierTypeId = identifierTypeId,
                IdentifierValue = identifierValue,
                EffectiveDate = command.EffectiveDate,
            });

            AddAuditEntry(
                $"identifier_{identifierTypeId}",
                null,
                identifierValue,
                id,
                changedAt);

            hasBusinessChanges = true;
        }

        if (hasBusinessChanges)
        {
            instrument.LastUpdated = DateOnly.FromDateTime(changedAt);
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var conflictDetails = await BuildUpdateConflictDetailsAsync(
                id,
                normalizedIsin,
                validatedAdditionalIdentifiers,
                cancellationToken);

            return UpdateInstrumentResult.Conflict(conflictDetails);
        }

        return UpdateInstrumentResult.Updated();
    }

    private async Task<string> BuildUpdateConflictDetailsAsync(
        string instrumentId,
        string normalizedIsin,
        IReadOnlyCollection<(string IdentifierTypeId, string IdentifierValue)> additionalIdentifiers,
        CancellationToken cancellationToken)
    {
        var conflicts = new List<string>();

        var primaryIsinExists = await dbContext.Instruments
            .AsNoTracking()
            .AnyAsync(item => item.InstrumentId != instrumentId && item.PrimaryIsin == normalizedIsin, cancellationToken);
        if (primaryIsinExists)
        {
            conflicts.Add($"PrimaryIsin '{normalizedIsin}'");
        }

        foreach (var additionalIdentifier in additionalIdentifiers)
        {
            var exists = await dbContext.InstrumentIdentifiers
                .AsNoTracking()
                .AnyAsync(
                    item => item.InstrumentId != instrumentId
                        && item.IdentifierTypeId == additionalIdentifier.IdentifierTypeId
                        && item.IdentifierValue == additionalIdentifier.IdentifierValue,
                    cancellationToken);

            if (exists)
            {
                conflicts.Add($"{additionalIdentifier.IdentifierTypeId} ({additionalIdentifier.IdentifierValue})");
            }
        }

        if (conflicts.Count == 0)
        {
            return "A unique constraint was violated while updating the instrument.";
        }

        return $"Duplicate value(s) detected for: {string.Join(", ", conflicts)}.";
    }

    private void AddAuditEntry(
        string fieldName,
        string? oldValue,
        string? newValue,
        string instrumentId,
        DateTime changedAt)
    {
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

        AddAuditEntry(fieldName, oldValue, newValue, instrumentId, changedAt);

        return true;
    }
}
