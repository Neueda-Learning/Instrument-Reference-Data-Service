namespace InstrumentReferenceDataService.Models;

public class InstrumentAudit
{
    public string AuditId { get; set; } = null!;
    public string InstrumentId { get; set; } = null!;
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = null!;
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangeSource { get; set; } = null!;

    public Instrument Instrument { get; set; } = null!;
}