DELIMITER $$



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