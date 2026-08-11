namespace InstrumentReferenceDataService.Models;

public class Sector
{
    public int SectorId { get; set; }
    public string SectorName { get; set; } = null!;

    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}