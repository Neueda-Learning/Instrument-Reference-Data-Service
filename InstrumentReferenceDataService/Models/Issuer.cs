namespace InstrumentReferenceDataService.Models;

public class Issuer
{
    public int IssuerId { get; set; }
    public string IssuerName { get; set; } = null!;

    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}