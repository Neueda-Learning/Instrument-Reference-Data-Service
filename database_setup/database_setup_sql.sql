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

