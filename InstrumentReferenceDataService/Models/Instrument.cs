namespace InstrumentReferenceDataService.Models;

public class Instrument
{
    public string InstrumentId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string PrimaryIsin { get; set; } = null!;
    public string AssetClassId { get; set; } = null!;
    public int SectorId { get; set; }
    public int ExchangeId { get; set; }
    public int CurrencyId { get; set; }
    public int IssuerId { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly LastUpdated { get; set; }

    public AssetClass AssetClass { get; set; } = null!;
    public Sector Sector { get; set; } = null!;
    public Exchange Exchange { get; set; } = null!;
    public Currency Currency { get; set; } = null!;
    public Issuer Issuer { get; set; } = null!;
    public ICollection<InstrumentIdentifier> Identifiers { get; set; } = new List<InstrumentIdentifier>();
    public ICollection<InstrumentAudit> Audits { get; set; } = new List<InstrumentAudit>();
}