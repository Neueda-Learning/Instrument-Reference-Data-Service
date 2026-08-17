# Instrument Reference Data Service

ASP.NET Core Web API backed by MySQL via Entity Framework Core.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)
- MySQL 8.0+
- [EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
dotnet tool install --global dotnet-ef
```

## Setup

### 1. Clone the repository

```bash
git clone <repository-url>
cd training-project
```

### 2. Download libraries
```bash
dotnet restore
```

### 3. Configure the database connection

Connection strings are managed via [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) and are never committed to source control.

```bash
cd InstrumentReferenceDataService
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=instrument_reference_db;User=<user>;Password=<password>;"
```

### 4. Apply database migrations

```bash
dotnet ef database update
```

### 5. Run the application

```bash
dotnet run
```
### 6. Run the tests

```bash

cd InstrumentReferenceDataService.Tests

dotnet test
```

### 7. Run the frontend (React + Vite)

```bash
cd frontend
npm install
npm run dev
```

The frontend starts at `http://localhost:5173` by default.
If port `5173` is already in use, Vite will automatically use the next available port (for example, `http://localhost:5174`).

The API will be available at `http://localhost:5105`.  
The OpenAPI/Swagger spec is available at `http://localhost:5105/swagger/ui` (when built in Debug mode).

## API Documentation

### Base URL
```
http://localhost:5105/api
```

### Endpoints

#### 1. Get All Instruments
Retrieve all instruments or filter by identifier (ISIN, CUSIP).

**Request:**
```bash
# Get all instruments
curl -X GET "http://localhost:5105/api/instruments" \
  -H "Accept: application/json"

# Filter by ISIN
curl -X GET "http://localhost:5105/api/instruments?isin=US0000000001" \
  -H "Accept: application/json"

# Filter by CUSIP
curl -X GET "http://localhost:5105/api/instruments?cusip=000000001" \
  -H "Accept: application/json"
```

**Response (200 OK):**
```json
[
  {
    "instrument": {
      "instrumentId": "INS-20260811131902-0001",
      "name": "Apple Inc. Stock",
      "primaryIsin": "US0000000001",
      "assetClassId": "EQ",
      "assetClassName": "Equity",
      "sectorId": 1,
      "sectorName": "Technology",
      "exchangeId": 1,
      "exchangeMicCode": "XNYS",
      "exchangeName": "New York Stock Exchange",
      "currencyId": 1,
      "currencyName": "USD",
      "issuerId": 1,
      "issuerName": "Apple Inc.",
      "status": "Active",
      "effectiveDate": "2026-08-11",
      "lastUpdated": "2026-08-11"
    },
    "identifiers": [
      {
        "identifierId": "ID-001",
        "identifierTypeId": "ISIN",
        "identifierTypeName": "International Securities Identification Number",
        "identifierValue": "US0000000001",
        "effectiveDate": "2026-08-11",
        "expiryDate": null
      },
      {
        "identifierId": "ID-002",
        "identifierTypeId": "CUSIP",
        "identifierTypeName": "Committee on Uniform Security Identification Procedures",
        "identifierValue": "000000001",
        "effectiveDate": "2026-08-11",
        "expiryDate": null
      }
    ],
    "audits": [
      {
        "auditId": "AUD-001",
        "changedAt": "2026-08-11T10:00:00Z",
        "changedBy": "system.seed",
        "fieldName": "status",
        "oldValue": null,
        "newValue": "Active",
        "changeSource": "MockGenerator"
      }
    ]
  }
]
```

---

#### Contract Lookup Endpoints

The API contract includes dedicated lookup routes:

- `GET /api/instruments/lookup?isin={isin}`
- `GET /api/instruments/lookup?cusip={cusip}`

Current implementation behavior:

- `GET /api/instruments?isin={isin}`
- `GET /api/instruments?cusip={cusip}`

Equivalent request examples:

```bash
# Contract-style lookup by ISIN
curl -X GET "http://localhost:5105/api/instruments/lookup?isin=US0000000001" \
  -H "Accept: application/json"

# Contract-style lookup by CUSIP
curl -X GET "http://localhost:5105/api/instruments/lookup?cusip=000000001" \
  -H "Accept: application/json"

# Current implementation lookup by ISIN
curl -X GET "http://localhost:5105/api/instruments?isin=US0000000001" \
  -H "Accept: application/json"

# Current implementation lookup by CUSIP
curl -X GET "http://localhost:5105/api/instruments?cusip=000000001" \
  -H "Accept: application/json"
```

---

#### 2. Get Instrument by ID
Retrieve a specific instrument by its ID.

**Request:**
```bash
curl -X GET "http://localhost:5105/api/instruments/INS-20260811131902-0001" \
  -H "Accept: application/json"
```

**Response (200 OK):**
```json
{
  "instrument": {
    "instrumentId": "INS-20260811131902-0001",
    "name": "Apple Inc. Stock",
    "primaryIsin": "US0000000001",
    "assetClassId": "EQ",
    "assetClassName": "Equity",
    "sectorId": 1,
    "sectorName": "Technology",
    "exchangeId": 1,
    "exchangeMicCode": "XNYS",
    "exchangeName": "New York Stock Exchange",
    "currencyId": 1,
    "currencyName": "USD",
    "issuerId": 1,
    "issuerName": "Apple Inc.",
    "status": "Active",
    "effectiveDate": "2026-08-11",
    "lastUpdated": "2026-08-11"
  },
  "identifiers": [...],
  "audits": [...]
}
```

**Response (404 Not Found):**
```json
null
```

---

#### 3. Create Instrument
Create a new instrument with validation.

**Request:**
```bash
curl -X POST "http://localhost:5105/api/instruments" \
  -H "Content-Type: application/json" \
  -d '{
    "instrumentId": "INS-20260817-0002",
    "name": "Microsoft Corporation Stock",
    "primaryIsin": "US5949181045",
    "assetClassId": "EQ",
    "sectorId": 1,
    "exchangeId": 1,
    "currencyId": 1,
    "issuerId": 2,
    "status": "Active",
    "effectiveDate": "2026-08-17"
  }'
```

**Response (201 Created):**
```json
{
  "instrumentId": "INS-20260817-0002",
  "name": "Microsoft Corporation Stock",
  "primaryIsin": "US5949181045",
  "assetClassId": "EQ",
  "assetClassName": "Equity",
  "sectorId": 1,
  "sectorName": "Technology",
  "exchangeId": 1,
  "exchangeMicCode": "XNYS",
  "exchangeName": "New York Stock Exchange",
  "currencyId": 1,
  "currencyName": "USD",
  "issuerId": 2,
  "issuerName": "Microsoft Inc.",
  "status": "Active",
  "effectiveDate": "2026-08-17",
  "lastUpdated": "2026-08-17"
}
```

**Response (400 Bad Request):**
```json
{
  "errors": {
    "PrimaryIsin": ["'Primary Isin' must be exactly 12 characters in length."],
    "InstrumentId": ["The InstrumentId field is required."]
  }
}
```

**Response (409 Conflict):**
```json
{
  "message": "An instrument with the same ISIN already exists."
}
```

---

#### 4. Update Instrument
Update mutable instrument fields.

**Request:**
```bash
curl -X PUT "http://localhost:5105/api/instruments/INS-20260811131902-0001" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Apple Inc. Updated Stock",
    "assetClassId": "EQ",
    "sectorId": 1,
    "exchangeId": 1,
    "currencyId": 1,
    "issuerId": 1,
    "status": "Active",
    "effectiveDate": "2026-08-17"
  }'
```

**Response (204 No Content):**
```
(empty body)
```

**Response (404 Not Found):**
```
(empty body)
```

---

#### 5. Get Instrument Audit History
Retrieve full change history for a specific instrument.

**Request:**
```bash
curl -X GET "http://localhost:5105/api/instruments/INS-20260811131902-0001/audit" \
  -H "Accept: application/json"
```

**Response (200 OK):**
```json
[
  {
    "auditId": "AUD-001",
    "changedAt": "2026-08-11T10:00:00Z",
    "changedBy": "system.seed",
    "fieldName": "status",
    "oldValue": "Pending",
    "newValue": "Active",
    "changeSource": "MockGenerator"
  }
]
```

**Response (404 Not Found):**
```
(empty body)
```

---

#### 6. Delete Instrument
Remove an instrument (cascades to identifiers and audits).

**Request:**
```bash
curl -X DELETE "http://localhost:5105/api/instruments/INS-20260811131902-0001"
```

**Response (204 No Content):**
```
(empty body)
```

**Response (404 Not Found):**
```
(empty body)
```

---

#### 7. Get Quality Report
Retrieve instruments that fail data quality checks.

**Request:**
```bash
curl -X GET "http://localhost:5105/api/instruments/quality-report" \
  -H "Accept: application/json"
```

**Response (200 OK):**
```json
[
  {
    "instrumentId": "INS-20260811131902-0001",
    "name": "Apple Inc. Stock",
    "primaryIsin": "INVALID123",
    "failingIndicators": [
      {
        "code": "PRIMARY_ISIN_FORMAT_INVALID",
        "description": "Primary ISIN does not match the expected 12-character ISIN format."
      },
      {
        "code": "STATUS_MISSING",
        "description": "Instrument status is null, empty, or whitespace."
      },
      {
        "code": "EFFECTIVE_DATE_AFTER_LAST_UPDATED",
        "description": "EffectiveDate is later than LastUpdated."
      }
    ]
  }
]
```

---

### Health / Readiness

#### Health Check Endpoint

**Endpoint:** `GET /health`

Use this endpoint to verify service readiness.

**Request:**
```bash
curl -X GET "http://localhost:5105/health"
```

**Response (200 OK):**
```
Healthy
```

---

### Mock Data

#### Generate Mock Data

**Endpoint:** `POST /api/mock-data/generate`

Generate test instruments and populate the database.

**Parameters:**
- `count` (optional, integer): Number of instruments to generate. Default: 50
- `seed` (optional, integer): Random seed for deterministic generation

**Request:**
```bash
# Generate 50 instruments with default settings
curl -X POST "http://localhost:5105/api/mock-data/generate"

# Generate 20 instruments with seed 123
curl -X POST "http://localhost:5105/api/mock-data/generate?count=20&seed=123"

# Generate 100 instruments
curl -X POST "http://localhost:5105/api/mock-data/generate?count=100"
```

**Response (200 OK):**
```json
{
  "message": "Generated 20 instruments successfully"
}
```

---

## HTTP Status Codes

| Status | Meaning |
|--------|---------|
| 200 | Success |
| 201 | Created |
| 204 | No Content (successful delete) |
| 400 | Bad Request (validation error) |
| 404 | Not Found |
| 409 | Conflict (duplicate ISIN) |
| 500 | Internal Server Error |

---

## Build & Run

### Build the project
```bash
cd InstrumentReferenceDataService
dotnet build
```

### Run in Development
```bash
dotnet run
```

### Run Tests
```bash
cd ../InstrumentReferenceDataService.Tests
dotnet test
```

### Run with Release Configuration
```bash
cd InstrumentReferenceDataService
dotnet run --configuration Release
```

