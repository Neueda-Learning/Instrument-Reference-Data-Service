namespace InstrumentReferenceDataService.Models;

public class Currency
{
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; } = null!;

    public ICollection<Exchange> Exchanges { get; set; } = new List<Exchange>();
    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}