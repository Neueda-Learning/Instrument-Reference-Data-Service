
CREATE VIEW active_instruments_vw AS

SELECT

    i.instrument_id,
    i.name,
    i.primary_isin,

    ac.asset_class_id,
    ac.name AS asset_class_name,

    s.sector_id,
    s.sector_name,

    e.exchange_id,
    e.mic_code,
    e.exchange_name,

    c.currency_id,
    c.currency_name,

    iss.issuer_id,
    iss.issuer_name,

    i.status,
    i.effective_date,
    i.last_updated

FROM Instrument i

JOIN AssetClass ac
    ON i.asset_class_id = ac.asset_class_id

LEFT JOIN Sector s
    ON i.sector_id = s.sector_id

JOIN `Exchange` e
    ON i.exchange_id = e.exchange_id

JOIN Currency c
    ON i.currency_id = c.currency_id

JOIN Issuer iss
    ON i.issuer_id = iss.issuer_id

WHERE i.status = 'ACTIVE';


CREATE VIEW instruments_by_asset_class_vw AS

SELECT

    ac.asset_class_id,

    ac.name AS asset_class_name,

    ac.description,

    COUNT(i.instrument_id)
        AS instrument_count,

    GROUP_CONCAT(
        i.name
        ORDER BY i.name
        SEPARATOR ', '
    ) AS instrument_names

FROM AssetClass ac

LEFT JOIN Instrument i
    ON ac.asset_class_id =
       i.asset_class_id

GROUP BY

    ac.asset_class_id,
    ac.name,
    ac.description;


CREATE VIEW stale_instruments_vw AS

SELECT

    i.instrument_id,

    i.name,

    i.primary_isin,

    i.status,

    i.last_updated,

    DATEDIFF(
        CURRENT_DATE,
        i.last_updated
    ) AS days_stale

FROM Instrument i

WHERE i.status = 'ACTIVE'

AND i.last_updated <
    CURRENT_DATE - INTERVAL 30 DAY;


