namespace InstrumentReferenceDataService.Models;

public class IdentifierType
{
    public string IdentifierTypeId { get; set; } = null!;
    public string IdentifierTypeName { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<InstrumentIdentifier> InstrumentIdentifiers { get; set; } = new List<InstrumentIdentifier>();
}