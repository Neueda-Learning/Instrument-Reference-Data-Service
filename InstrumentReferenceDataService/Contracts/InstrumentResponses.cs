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

public sealed record PagedResultResponse<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);

public sealed record MonitoringInstrumentItemResponse(
    string InstrumentId,
    string Name,
    DateOnly LastUpdated,
    int AgeDays);

public sealed record MonitoringAnomalyItemResponse(
    string InstrumentId,
    string Name,
    DateOnly LastUpdated,
    string Reason);

public sealed record MonitoringDataResponse(
    int FreshnessScore,
    PagedResultResponse<MonitoringInstrumentItemResponse> Stale,
    PagedResultResponse<MonitoringInstrumentItemResponse> Recent,
    PagedResultResponse<MonitoringAnomalyItemResponse> Anomalies);

public sealed record InstrumentQualityIndicatorResponse(
    string Code,
    string Description);

public sealed record InstrumentQualityReportItemResponse(
    string InstrumentId,
    string Name,
    string PrimaryIsin,
    IReadOnlyCollection<InstrumentQualityIndicatorResponse> FailingIndicators);

public sealed record AssetClassOptionResponse(
    string AssetClassId,
    string Name);

public sealed record SectorOptionResponse(
    int SectorId,
    string Name);

public sealed record ExchangeOptionResponse(
    int ExchangeId,
    string MicCode,
    string Name);

public sealed record CurrencyOptionResponse(
    int CurrencyId,
    string Name);

public sealed record IssuerOptionResponse(
    int IssuerId,
    string Name);

public sealed record StatusOptionResponse(
    string Value);

public sealed record InstrumentEditOptionsResponse(
    IReadOnlyCollection<AssetClassOptionResponse> AssetClasses,
    IReadOnlyCollection<SectorOptionResponse> Sectors,
    IReadOnlyCollection<ExchangeOptionResponse> Exchanges,
    IReadOnlyCollection<CurrencyOptionResponse> Currencies,
    IReadOnlyCollection<IssuerOptionResponse> Issuers,
    IReadOnlyCollection<StatusOptionResponse> Statuses);

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