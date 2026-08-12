using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Models;

namespace InstrumentReferenceDataService.Extensions;

public static class InstrumentQueryExtensions
{
    public static IQueryable<InstrumentSummaryResponse> SelectInstrumentSummary(this IQueryable<Instrument> query)
    {
        return query.Select(instrument => new InstrumentSummaryResponse(
            instrument.InstrumentId,
            instrument.Name,
            instrument.PrimaryIsin,
            instrument.AssetClassId,
            instrument.AssetClass.Name,
            instrument.SectorId,
            instrument.Sector.SectorName,
            instrument.ExchangeId,
            instrument.Exchange.MicCode,
            instrument.Exchange.ExchangeName,
            instrument.CurrencyId,
            instrument.Currency.CurrencyName,
            instrument.IssuerId,
            instrument.Issuer.IssuerName,
            instrument.Status,
            instrument.EffectiveDate,
            instrument.LastUpdated));
    }

    public static IQueryable<InstrumentIdentifierResponse> SelectIdentifierResponse(this IQueryable<InstrumentIdentifier> query)
    {
        return query.Select(identifier => new InstrumentIdentifierResponse(
            identifier.IdentifierId,
            identifier.IdentifierTypeId,
            identifier.IdentifierType.IdentifierTypeName,
            identifier.IdentifierValue,
            identifier.EffectiveDate,
            identifier.ExpiryDate));
    }

    public static IQueryable<InstrumentAuditResponse> SelectAuditResponse(this IQueryable<InstrumentAudit> query)
    {
        return query.Select(audit => new InstrumentAuditResponse(
            audit.AuditId,
            audit.ChangedAt,
            audit.ChangedBy,
            audit.FieldName,
            audit.OldValue,
            audit.NewValue,
            audit.ChangeSource));
    }
}