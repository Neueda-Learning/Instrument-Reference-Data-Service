
CREATE INDEX IX_Instrument_AssetClass
    ON Instrument(asset_class_id);

CREATE INDEX IX_Instrument_Sector
    ON Instrument(sector_id);

CREATE INDEX IX_Instrument_Exchange
    ON Instrument(exchange_id);

CREATE INDEX IX_Instrument_Currency
    ON Instrument(currency_id);

CREATE INDEX IX_Instrument_Issuer
    ON Instrument(issuer_id);

CREATE INDEX IX_Instrument_Status
    ON Instrument(status);

CREATE INDEX IX_Instrument_LastUpdated
    ON Instrument(last_updated);

CREATE INDEX IX_InstrumentIdentifier_Instrument
    ON InstrumentIdentifier(instrument_id);

CREATE INDEX IX_InstrumentIdentifier_Type
    ON InstrumentIdentifier(id_type_id);

CREATE INDEX IX_InstrumentIdentifier_Value
    ON InstrumentIdentifier(identifier_value);

CREATE INDEX IX_InstrumentAudit_Instrument
    ON InstrumentAudit(instrument_id);

CREATE INDEX IX_InstrumentAudit_ChangedAt
    ON InstrumentAudit(changed_at);

