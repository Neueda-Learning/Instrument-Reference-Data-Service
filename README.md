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

The API will be available at `https://localhost:5105`.  
The OpenAPI spec is served at `https://localhost:5105/openapi/v1.json`.

## Mock Data

### Generate data

Send a `POST` request to seed the database. `count` defaults to 50; `seed` is optional and makes generation deterministic.

```bash
curl -X POST "http://localhost:5105/api/mock-data/generate?count=50&seed=123"
```

### Retrieve instruments

```bash
curl "http://localhost:5105/api/instruments"
```

Optional query parameters: `status`, `assetClassId`, `exchangeId`, `issuerId`, `skip`, `take` (max 200).
