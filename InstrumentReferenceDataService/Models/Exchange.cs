namespace InstrumentReferenceDataService.Models;

public class Exchange
{
    public int ExchangeId { get; set; }
    public string MicCode { get; set; } = null!;
    public string ExchangeName { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Timezone { get; set; } = null!;
    public int CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;
    public ICollection<Instrument> Instruments { get; set; } = new List<Instrument>();
}