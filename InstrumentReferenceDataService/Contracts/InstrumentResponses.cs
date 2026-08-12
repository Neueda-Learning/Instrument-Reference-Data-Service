namespace InstrumentReferenceDataService.Contracts;

public sealed record InstrumentSummaryResponse(
    string InstrumentId,
    string Name,
    string PrimaryIsin,
    string AssetClassId,
    string AssetClassName,
    int SectorId,
    string SectorName,
    int ExchangeId,
    string ExchangeMicCode,
    string ExchangeName,
    int CurrencyId,
    string CurrencyName,
    int IssuerId,
    string IssuerName,
    string Status,
    DateOnly EffectiveDate,
    DateOnly LastUpdated);

public sealed record InstrumentIdentifierResponse(
    string IdentifierId,
    string IdentifierTypeId,
    string IdentifierTypeName,
    string IdentifierValue,
    DateOnly EffectiveDate,
    DateOnly? ExpiryDate);

public sealed record InstrumentAuditResponse(
    string AuditId,
    DateTime ChangedAt,
    string ChangedBy,
    string FieldName,
    string? OldValue,
    string? NewValue,
    string ChangeSource);

public sealed record InstrumentDetailResponse(
    InstrumentSummaryResponse Instrument,
    IReadOnlyCollection<InstrumentIdentifierResponse> Identifiers,
    IReadOnlyCollection<InstrumentAuditResponse> Audits);