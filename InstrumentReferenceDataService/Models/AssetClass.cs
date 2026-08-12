namespace InstrumentReferenceDataService.Models;

public class AssetClass
{
    public string AssetClassId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}