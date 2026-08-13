using System.ComponentModel.DataAnnotations;

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
public sealed record CreateInstrumentRequest(
    [Required(AllowEmptyStrings = false)]
    [StringLength(40)]
    string InstrumentId,
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(150)]
    string Name,
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(12, MinimumLength = 12)]
    [RegularExpression("^[A-Z]{2}[A-Z0-9]{9}[0-9]$")]
    string PrimaryIsin,
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(32)]
    string AssetClassId,
    
    int SectorId,
    int ExchangeId,
    int CurrencyId,
    int IssuerId,
    
    [Required(AllowEmptyStrings = false)]
    [StringLength(32)]
    string Status,
    
    DateOnly EffectiveDate
);