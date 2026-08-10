
-- Project: 01 - Instrument Reference Data Service
-- Task: P1B-3 Write DDL Scripts for Core Tables



-- guys if you dont have this tables u dont need to run it
IF OBJECT_ID('InstrumentAudit', 'U') IS NOT NULL DROP TABLE InstrumentAudit;
IF OBJECT_ID('InstrumentIdentifier', 'U') IS NOT NULL DROP TABLE InstrumentIdentifier;
IF OBJECT_ID('Instrument', 'U') IS NOT NULL DROP TABLE Instrument;
IF OBJECT_ID('IdentifierType', 'U') IS NOT NULL DROP TABLE IdentifierType;
IF OBJECT_ID('Issuer', 'U') IS NOT NULL DROP TABLE Issuer;
IF OBJECT_ID('Exchange', 'U') IS NOT NULL DROP TABLE Exchange;
IF OBJECT_ID('Sector', 'U') IS NOT NULL DROP TABLE Sector;
IF OBJECT_ID('AssetClass', 'U') IS NOT NULL DROP TABLE AssetClass;
IF OBJECT_ID('Currency', 'U') IS NOT NULL DROP TABLE Currency;
GO

-- 2. creating reference tables

CREATE TABLE Currency (
    currency_id CHAR(3) PRIMARY KEY, 
    name VARCHAR(50) NOT NULL
);

CREATE TABLE AssetClass (
    asset_class_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE Sector (
    sector_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255)
);

CREATE TABLE Exchange (
    exchange_id INT IDENTITY(1,1) PRIMARY KEY,
    mic_code CHAR(4) NOT NULL UNIQUE, 
    exchange_name VARCHAR(100) NOT NULL,
    country CHAR(2) NOT NULL,
    timezone VARCHAR(50) NOT NULL,
    currency CHAR(3) NOT NULL
);

CREATE TABLE Issuer (
    issuer_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    country CHAR(2) NOT NULL
);

CREATE TABLE IdentifierType (
    id_type_id INT IDENTITY(1,1) PRIMARY KEY,
    id_type_name VARCHAR(50) NOT NULL, 
    description VARCHAR(255)
);
GO


-- 3. creating core entity tables 


CREATE TABLE Instrument (
    instrument_id INT IDENTITY(1,1) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    primary_isin CHAR(12) NOT NULL UNIQUE,
    asset_class_id INT NOT NULL,
    sector_id INT NOT NULL,
    exchange_id INT NOT NULL,
    currency_id CHAR(3) NOT NULL,
    issuer_id INT NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE', 
    effective_date DATE NOT NULL,
    last_updated DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    
    -- foreign Keys
    CONSTRAINT FK_Instrument_AssetClass FOREIGN KEY (asset_class_id) REFERENCES AssetClass(asset_class_id),
    CONSTRAINT FK_Instrument_Sector FOREIGN KEY (sector_id) REFERENCES Sector(sector_id),
    CONSTRAINT FK_Instrument_Exchange FOREIGN KEY (exchange_id) REFERENCES Exchange(exchange_id),
    CONSTRAINT FK_Instrument_Currency FOREIGN KEY (currency_id) REFERENCES Currency(currency_id),
    CONSTRAINT FK_Instrument_Issuer FOREIGN KEY (issuer_id) REFERENCES Issuer(issuer_id)
);
GO


-- 4. creating dependent tables

CREATE TABLE InstrumentIdentifier (
    identifier_id INT IDENTITY(1,1) PRIMARY KEY,
    instrument_id INT NOT NULL,
    id_type_id INT NOT NULL,
    identifier_value VARCHAR(100) NOT NULL,
    effective_date DATE NOT NULL,
    expiry_date DATE NULL, 
    
    -- foreign keys
    CONSTRAINT FK_InstId_Instrument FOREIGN KEY (instrument_id) REFERENCES Instrument(instrument_id),
    CONSTRAINT FK_InstId_IdType FOREIGN KEY (id_type_id) REFERENCES IdentifierType(id_type_id),
    CONSTRAINT UQ_InstId_TypeValue UNIQUE (id_type_id, identifier_value) 
);

CREATE TABLE InstrumentAudit (
    audit_id INT IDENTITY(1,1) PRIMARY KEY,
    instrument_id INT NOT NULL,
    changed_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    changed_by VARCHAR(100) NOT NULL,
    field_name VARCHAR(50) NOT NULL,
    old_value VARCHAR(MAX) NULL,
    new_value VARCHAR(MAX) NULL,
    change_source VARCHAR(100) NOT NULL,
    
    -- foreign Keys
    CONSTRAINT FK_Audit_Instrument FOREIGN KEY (instrument_id) REFERENCES Instrument(instrument_id)
);
GO