namespace InstrumentReferenceDataService.Models;

public class InstrumentIdentifier
{
    public string IdentifierId { get; set; } = null!;
    public string InstrumentId { get; set; } = null!;
    public string IdentifierTypeId { get; set; } = null!;
    public string IdentifierValue { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    public Instrument Instrument { get; set; } = null!;
    public IdentifierType IdentifierType { get; set; } = null!;
}