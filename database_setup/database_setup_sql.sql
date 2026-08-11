DROP VIEW IF EXISTS active_instruments_vw;
DROP VIEW IF EXISTS instruments_by_asset_class_vw;
DROP VIEW IF EXISTS stale_instruments_vw;

DROP PROCEDURE IF EXISTS upsert_instrument;
DROP PROCEDURE IF EXISTS deactivate_instrument;
DROP PROCEDURE IF EXISTS get_by_identifier;
DROP PROCEDURE IF EXISTS get_audit_history;

SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS InstrumentAudit;
DROP TABLE IF EXISTS InstrumentIdentifier;
DROP TABLE IF EXISTS Instrument;
DROP TABLE IF EXISTS IdentifierType;
DROP TABLE IF EXISTS `Exchange`;
DROP TABLE IF EXISTS Issuer;
DROP TABLE IF EXISTS Currency;
DROP TABLE IF EXISTS Sector;
DROP TABLE IF EXISTS AssetClass;

SET FOREIGN_KEY_CHECKS = 1;

CREATE TABLE AssetClass (

    asset_class_id varchar(36) NOT NULL,

    name VARCHAR(50) NOT NULL,

    description VARCHAR(255),

    CONSTRAINT PK_AssetClass
        PRIMARY KEY (asset_class_id),

    CONSTRAINT UQ_AssetClass_Name
        UNIQUE (name)
);




CREATE TABLE Sector (

    sector_id INT NOT NULL AUTO_INCREMENT,

    sector_name VARCHAR(100) NOT NULL,

    CONSTRAINT PK_Sector
        PRIMARY KEY (sector_id),

    CONSTRAINT UQ_Sector_Name
        UNIQUE (sector_name)
);



CREATE TABLE Currency (

    currency_id INT NOT NULL AUTO_INCREMENT,

    currency_name VARCHAR(50) NOT NULL,

    CONSTRAINT PK_Currency
        PRIMARY KEY (currency_id),

    CONSTRAINT UQ_Currency_Name
        UNIQUE (currency_name)
);




CREATE TABLE Issuer (

    issuer_id INT NOT NULL AUTO_INCREMENT,

    issuer_name VARCHAR(150) NOT NULL,

    CONSTRAINT PK_Issuer
        PRIMARY KEY (issuer_id),

    CONSTRAINT UQ_Issuer_Name
        UNIQUE (issuer_name)
);



CREATE TABLE `Exchange` (

    exchange_id INT NOT NULL AUTO_INCREMENT,

    mic_code VARCHAR(10) NOT NULL,

    exchange_name VARCHAR(150) NOT NULL,

    country VARCHAR(100) NOT NULL,

    timezone VARCHAR(100) NOT NULL,

    currency_id INT NOT NULL,

    CONSTRAINT PK_Exchange
        PRIMARY KEY (exchange_id),

    CONSTRAINT UQ_Exchange_MIC
        UNIQUE (mic_code),

    CONSTRAINT FK_Exchange_Currency
        FOREIGN KEY (currency_id)
        REFERENCES Currency(currency_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);



CREATE TABLE IdentifierType (

    id_type_id VARCHAR(36) NOT NULL,

    id_type_name VARCHAR(50) NOT NULL,

    description VARCHAR(255),

    CONSTRAINT PK_IdentifierType
        PRIMARY KEY (id_type_id),

    CONSTRAINT UQ_IdentifierType_Name
        UNIQUE (id_type_name)
);


CREATE TABLE Instrument (

    instrument_id VARCHAR(36) NOT NULL,

    name VARCHAR(200) NOT NULL,

    primary_isin VARCHAR(20) NOT NULL,

    asset_class_id VARCHAR(36) NOT NULL,

    sector_id INT NULL,

    exchange_id INT NOT NULL,

    currency_id INT NOT NULL,

    issuer_id INT NOT NULL,

    status VARCHAR(20) NOT NULL
        DEFAULT 'ACTIVE',

    effective_date DATE NOT NULL,

    last_updated DATE NOT NULL
        DEFAULT (CURRENT_DATE),

    CONSTRAINT PK_Instrument
        PRIMARY KEY (instrument_id),

    CONSTRAINT UQ_Instrument_PrimaryISIN
        UNIQUE (primary_isin),

    CONSTRAINT FK_Instrument_AssetClass
        FOREIGN KEY (asset_class_id)
        REFERENCES AssetClass(asset_class_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Instrument_Sector
        FOREIGN KEY (sector_id)
        REFERENCES Sector(sector_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Instrument_Exchange
        FOREIGN KEY (exchange_id)
        REFERENCES `Exchange`(exchange_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Instrument_Currency
        FOREIGN KEY (currency_id)
        REFERENCES Currency(currency_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_Instrument_Issuer
        FOREIGN KEY (issuer_id)
        REFERENCES Issuer(issuer_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT CK_Instrument_Status
        CHECK (
            status IN ('ACTIVE', 'INACTIVE')
        )
);




CREATE TABLE InstrumentIdentifier (

    identifier_id VARCHAR(36) NOT NULL,

    instrument_id VARCHAR(36) NOT NULL,

    id_type_id VARCHAR(36) NOT NULL,

    identifier_value VARCHAR(100) NOT NULL,

    effective_date DATE NOT NULL,

    expiry_date DATE NULL,

    CONSTRAINT PK_InstrumentIdentifier
        PRIMARY KEY (identifier_id),

    CONSTRAINT FK_InstrumentIdentifier_Instrument
        FOREIGN KEY (instrument_id)
        REFERENCES Instrument(instrument_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT FK_InstrumentIdentifier_Type
        FOREIGN KEY (id_type_id)
        REFERENCES IdentifierType(id_type_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT,

    CONSTRAINT UQ_InstrumentIdentifier
        UNIQUE (id_type_id, identifier_value),

    CONSTRAINT CK_InstrumentIdentifier_Dates
        CHECK (
            expiry_date IS NULL
            OR expiry_date >= effective_date
        )
);



CREATE TABLE InstrumentAudit (

    audit_id VARCHAR(36) NOT NULL,

    instrument_id VARCHAR(36) NOT NULL,

    changed_at DATETIME NOT NULL
        DEFAULT CURRENT_TIMESTAMP,

    changed_by VARCHAR(100) NOT NULL,

    field_name VARCHAR(100) NOT NULL,

    old_value VARCHAR(500),

    new_value VARCHAR(500),

    change_source VARCHAR(100) NOT NULL,

    CONSTRAINT PK_InstrumentAudit
        PRIMARY KEY (audit_id),

    CONSTRAINT FK_InstrumentAudit_Instrument
        FOREIGN KEY (instrument_id)
        REFERENCES Instrument(instrument_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);



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


-- ============================================================
-- 17. STALE INSTRUMENTS VIEW
-- ============================================================

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


-- ============================================================
-- 18. PROCEDURES
-- ============================================================

DELIMITER $$


-- ============================================================
-- GET BY IDENTIFIER
-- ============================================================

CREATE PROCEDURE get_by_identifier (

    IN p_id_type VARCHAR(50),

    IN p_identifier_value VARCHAR(100)

)

BEGIN

    SELECT DISTINCT

        i.instrument_id,

        i.name,

        i.primary_isin,

        ac.name AS asset_class,

        s.sector_name,

        e.mic_code,

        e.exchange_name,

        c.currency_name,

        iss.issuer_name,

        i.status,

        i.effective_date,

        i.last_updated

    FROM Instrument i

    JOIN AssetClass ac
        ON i.asset_class_id =
           ac.asset_class_id

    LEFT JOIN Sector s
        ON i.sector_id =
           s.sector_id

    JOIN `Exchange` e
        ON i.exchange_id =
           e.exchange_id

    JOIN Currency c
        ON i.currency_id =
           c.currency_id

    JOIN Issuer iss
        ON i.issuer_id =
           iss.issuer_id

    WHERE

        (
            UPPER(p_id_type) = 'ISIN'
            AND i.primary_isin =
                p_identifier_value
        )

        OR EXISTS (

            SELECT 1

            FROM InstrumentIdentifier ii

            JOIN IdentifierType it
                ON ii.id_type_id =
                   it.id_type_id

            WHERE
                ii.instrument_id =
                i.instrument_id

            AND UPPER(it.id_type_name) =
                UPPER(p_id_type)

            AND ii.identifier_value =
                p_identifier_value

            AND (
                ii.expiry_date IS NULL
                OR ii.expiry_date >=
                   CURRENT_DATE
            )
        );

END $$

CREATE PROCEDURE get_audit_history (

    IN p_instrument_id VARCHAR(36)

)

BEGIN

    SELECT
        audit_id,
        instrument_id,
        changed_at,
        changed_by,
        field_name,
        old_value,
        new_value,
        change_source

    FROM InstrumentAudit

    WHERE instrument_id =
        p_instrument_id

    ORDER BY
        changed_at DESC,
        audit_id DESC;

END $$



CREATE PROCEDURE deactivate_instrument (

    IN p_instrument_id VARCHAR(36),

    IN p_reason VARCHAR(500),

    IN p_effective_date DATE,

    IN p_changed_by VARCHAR(100),

    IN p_change_source VARCHAR(100)

)

BEGIN

    DECLARE v_exists INT DEFAULT 0;

    DECLARE v_old_status VARCHAR(20);


    SELECT COUNT(*)

    INTO v_exists

    FROM Instrument

    WHERE instrument_id =
        p_instrument_id;


    IF v_exists = 0 THEN

        SIGNAL SQLSTATE '45000'

        SET MESSAGE_TEXT =
            'Instrument not found';

    END IF;


    SELECT status

    INTO v_old_status

    FROM Instrument

    WHERE instrument_id =
        p_instrument_id;


    IF v_old_status = 'INACTIVE' THEN

        SIGNAL SQLSTATE '45000'

        SET MESSAGE_TEXT =
            'Instrument is already inactive';

    END IF;


    START TRANSACTION;


    UPDATE Instrument

    SET
        status = 'INACTIVE',

        last_updated = CURRENT_DATE

    WHERE instrument_id =
        p_instrument_id;


    INSERT INTO InstrumentAudit
    (
        audit_id,
        instrument_id,
        changed_at,
        changed_by,
        field_name,
        old_value,
        new_value,
        change_source
    )

    VALUES
    (
        UUID(),
        p_instrument_id,
        CURRENT_TIMESTAMP,
        p_changed_by,
        'status',
        v_old_status,
        'INACTIVE',
        p_change_source
    );


    INSERT INTO InstrumentAudit
    (
        audit_id,
        instrument_id,
        changed_at,
        changed_by,
        field_name,
        old_value,
        new_value,
        change_source
    )

    VALUES
    (
        UUID(),
        p_instrument_id,
        CURRENT_TIMESTAMP,
        p_changed_by,
        'deactivation_reason',
        NULL,
        p_reason,
        p_change_source
    );


    INSERT INTO InstrumentAudit
    (
        audit_id,
        instrument_id,
        changed_at,
        changed_by,
        field_name,
        old_value,
        new_value,
        change_source
    )

    VALUES
    (
        UUID(),
        p_instrument_id,
        CURRENT_TIMESTAMP,
        p_changed_by,
        'deactivation_effective_date',
        NULL,
        DATE_FORMAT(
            p_effective_date,
            '%Y-%m-%d'
        ),
        p_change_source
    );


    COMMIT;

END $$



CREATE PROCEDURE upsert_instrument (

    IN p_instrument_id VARCHAR(36),

    IN p_name VARCHAR(200),

    IN p_primary_isin VARCHAR(20),

    IN p_asset_class_id VARCHAR(36),

    IN p_sector_id INT,

    IN p_exchange_id INT,

    IN p_currency_id INT,

    IN p_issuer_id INT,

    IN p_status VARCHAR(20),

    IN p_effective_date DATE,

    IN p_changed_by VARCHAR(100),

    IN p_change_source VARCHAR(100)

)

BEGIN

    DECLARE v_instrument_id VARCHAR(36);

    DECLARE v_old_name VARCHAR(200);

    DECLARE v_old_asset_class_id VARCHAR(36);

    DECLARE v_old_sector_id INT;

    DECLARE v_old_exchange_id INT;

    DECLARE v_old_currency_id INT;

    DECLARE v_old_issuer_id INT;

    DECLARE v_old_status VARCHAR(20);

    DECLARE v_old_effective_date DATE;


    SELECT instrument_id

    INTO v_instrument_id

    FROM Instrument

    WHERE primary_isin =
        p_primary_isin

    LIMIT 1;


    IF v_instrument_id IS NULL THEN


        SET v_instrument_id =
            COALESCE(
                NULLIF(
                    p_instrument_id,
                    ''
                ),
                UUID()
            );


        INSERT INTO Instrument
        (
            instrument_id,
            name,
            primary_isin,
            asset_class_id,
            sector_id,
            exchange_id,
            currency_id,
            issuer_id,
            status,
            effective_date,
            last_updated
        )

        VALUES
        (
            v_instrument_id,
            p_name,
            p_primary_isin,
            p_asset_class_id,
            p_sector_id,
            p_exchange_id,
            p_currency_id,
            p_issuer_id,
            p_status,
            p_effective_date,
            CURRENT_DATE
        );


        INSERT INTO InstrumentAudit
        (
            audit_id,
            instrument_id,
            changed_at,
            changed_by,
            field_name,
            old_value,
            new_value,
            change_source
        )

        VALUES
        (
            UUID(),
            v_instrument_id,
            CURRENT_TIMESTAMP,
            p_changed_by,
            'CREATE',
            NULL,
            'Instrument created',
            p_change_source
        );


    ELSE


        SELECT

            name,
            asset_class_id,
            sector_id,
            exchange_id,
            currency_id,
            issuer_id,
            status,
            effective_date

        INTO

            v_old_name,
            v_old_asset_class_id,
            v_old_sector_id,
            v_old_exchange_id,
            v_old_currency_id,
            v_old_issuer_id,
            v_old_status,
            v_old_effective_date

        FROM Instrument

        WHERE instrument_id =
            v_instrument_id;


        IF NOT (
            v_old_name <=> p_name
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'name',
                v_old_name,
                p_name,
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_asset_class_id
            <=>
            p_asset_class_id
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'asset_class_id',
                v_old_asset_class_id,
                p_asset_class_id,
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_sector_id
            <=>
            p_sector_id
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'sector_id',
                CAST(v_old_sector_id AS CHAR),
                CAST(p_sector_id AS CHAR),
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_exchange_id
            <=>
            p_exchange_id
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'exchange_id',
                CAST(v_old_exchange_id AS CHAR),
                CAST(p_exchange_id AS CHAR),
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_currency_id
            <=>
            p_currency_id
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'currency_id',
                CAST(v_old_currency_id AS CHAR),
                CAST(p_currency_id AS CHAR),
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_issuer_id
            <=>
            p_issuer_id
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'issuer_id',
                CAST(v_old_issuer_id AS CHAR),
                CAST(p_issuer_id AS CHAR),
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_status
            <=>
            p_status
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'status',
                v_old_status,
                p_status,
                p_change_source
            );

        END IF;


        IF NOT (
            v_old_effective_date
            <=>
            p_effective_date
        ) THEN

            INSERT INTO InstrumentAudit
            (
                audit_id,
                instrument_id,
                changed_at,
                changed_by,
                field_name,
                old_value,
                new_value,
                change_source
            )

            VALUES
            (
                UUID(),
                v_instrument_id,
                CURRENT_TIMESTAMP,
                p_changed_by,
                'effective_date',

                DATE_FORMAT(
                    v_old_effective_date,
                    '%Y-%m-%d'
                ),

                DATE_FORMAT(
                    p_effective_date,
                    '%Y-%m-%d'
                ),

                p_change_source
            );

        END IF;


        UPDATE Instrument

        SET
            name = p_name,
            asset_class_id =
                p_asset_class_id,
            sector_id =
                p_sector_id,
            exchange_id =
                p_exchange_id,
            currency_id =
                p_currency_id,
            issuer_id =
                p_issuer_id,
            status =
                p_status,
            effective_date =
                p_effective_date,
            last_updated =
                CURRENT_DATE

        WHERE instrument_id =
            v_instrument_id;


    END IF;


    SELECT
        v_instrument_id
        AS instrument_id;

END $$


DELIMITER ;

SELECT
    'database created successfully'
    AS result;