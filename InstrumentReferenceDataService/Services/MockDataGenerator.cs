using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Models;
using Microsoft.EntityFrameworkCore;

namespace InstrumentReferenceDataService.Services;

public sealed class MockDataGenerator
{
    private sealed record MockIssuerSeed(int IssuerId, string IssuerName);

    private static readonly MockIssuerSeed[] IssuerSeeds =
    [
        new(1, "Atlas Capital"),
        new(2, "BluePeak Holdings"),
        new(3, "Northwind Energy"),
        new(4, "Helios Biotech"),
        new(5, "Meridian Bank"),
        new(6, "Vertex Industrial"),
        new(7, "Summit Retail Group"),
        new(8, "Aurora Infrastructure"),
        new(9, "Sterling Health Systems"),
        new(10, "Pacific Data Networks")
    ];

    private static readonly Dictionary<string, string[]> AssetClassNameTemplates = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EQ"] = ["Common Stock", "Preferred Stock", "Ordinary Share"],
        ["FI"] = ["Senior Note", "Corporate Bond", "Medium Term Note"],
        ["ETF"] = ["Growth ETF", "Income ETF", "Sector ETF"],
        ["FX"] = ["Spot Pair", "Forward Contract", "Currency Basket"],
        ["DRV"] = ["Call Option", "Put Option", "Index Future"]
    };

    private static readonly string[] InstrumentStatuses = ["Active", "Pending", "Suspended", "Delisted"];
    private static readonly string[] AuditUsers = ["system.seed", "ops.user", "qa.user", "reference.admin"];
    private static readonly string[] AuditFields = ["status", "exchange_id", "currency_id", "last_updated"];
    private static readonly string[] AuditSources = ["MockGenerator", "BackfillJob", "ManualReview"];

    private readonly AppDbContext dbContext;

    public MockDataGenerator(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<MockDataGenerationResult> GenerateAsync(int instrumentCount, int? seed = null, CancellationToken cancellationToken = default)
    {
        instrumentCount = Math.Clamp(instrumentCount, 1, 5000);

        await EnsureReferenceDataAsync(cancellationToken);

        var assetClasses = await dbContext.AssetClasses.AsNoTracking().ToListAsync(cancellationToken);
        var sectors = await dbContext.Sectors.AsNoTracking().ToListAsync(cancellationToken);
        var exchanges = await dbContext.Exchanges.AsNoTracking().ToListAsync(cancellationToken);
        var currencies = await dbContext.Currencies.AsNoTracking().ToListAsync(cancellationToken);
        var issuers = await dbContext.Issuers.AsNoTracking().ToListAsync(cancellationToken);
        var identifierTypes = await dbContext.IdentifierTypes.AsNoTracking().ToListAsync(cancellationToken);

        var random = seed.HasValue ? new Random(seed.Value) : new Random();
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var runId = seed.HasValue
            ? $"S{seed.Value:X}-{BuildStableToken(Guid.NewGuid().ToString("N"), 6)}"
            : DateTime.UtcNow.ToString("yyMMddHHmmss");

        var instruments = new List<Instrument>(instrumentCount);
        var instrumentIdentifiers = new List<InstrumentIdentifier>(instrumentCount * identifierTypes.Count);
        var audits = new List<InstrumentAudit>(instrumentCount * 2);

        for (var index = 0; index < instrumentCount; index++)
        {
            var assetClass = assetClasses[random.Next(assetClasses.Count)];
            var sector = sectors[random.Next(sectors.Count)];
            var exchange = exchanges[random.Next(exchanges.Count)];
            var issuer = issuers[random.Next(issuers.Count)];
            var status = InstrumentStatuses[random.Next(InstrumentStatuses.Length)];
            var effectiveDate = today.AddDays(-random.Next(30, 3650));
            var updatedDate = effectiveDate.AddDays(random.Next(0, 365));
            var instrumentId = $"INS-{runId}-{index + 1:0000}";
            var isin = GenerateIsin(exchange.Country, instrumentId);
            var instrumentName = BuildInstrumentName(random, issuer.IssuerName, assetClass.AssetClassId, exchange.ExchangeName);

            var instrument = new Instrument
            {
                InstrumentId = instrumentId,
                Name = instrumentName,
                PrimaryIsin = isin,
                AssetClassId = assetClass.AssetClassId,
                SectorId = sector.SectorId,
                ExchangeId = exchange.ExchangeId,
                CurrencyId = exchange.CurrencyId,
                IssuerId = issuer.IssuerId,
                Status = status,
                EffectiveDate = effectiveDate,
                LastUpdated = updatedDate
            };

            instruments.Add(instrument);

            instrumentIdentifiers.AddRange(CreateIdentifiers(identifierTypes, instrumentId, isin, effectiveDate, exchange.MicCode, issuer.IssuerName, index));
            audits.AddRange(CreateAudits(random, instrumentId, status, exchange.MicCode, updatedDate));
        }

        await dbContext.Instruments.AddRangeAsync(instruments, cancellationToken);
        await dbContext.InstrumentIdentifiers.AddRangeAsync(instrumentIdentifiers, cancellationToken);
        await dbContext.InstrumentAudits.AddRangeAsync(audits, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MockDataGenerationResult(
            instruments.Count,
            instrumentIdentifiers.Count,
            audits.Count,
            assetClasses.Count,
            sectors.Count,
            exchanges.Count,
            currencies.Count,
            issuers.Count,
                identifierTypes.Count,
                seed);
    }

    private async Task EnsureReferenceDataAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsSqlite())
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        }
        else
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        }

        if (!await dbContext.AssetClasses.AnyAsync(cancellationToken))
        {
            await dbContext.AssetClasses.AddRangeAsync(
            [
                new AssetClass { AssetClassId = "EQ", Name = "Equity", Description = "Common and preferred shares" },
                new AssetClass { AssetClassId = "FI", Name = "Fixed Income", Description = "Bonds and credit products" },
                new AssetClass { AssetClassId = "ETF", Name = "ETF", Description = "Exchange traded funds" },
                new AssetClass { AssetClassId = "FX", Name = "Foreign Exchange", Description = "Currency instruments" },
                new AssetClass { AssetClassId = "DRV", Name = "Derivative", Description = "Options and futures" }
            ], cancellationToken);
        }

        if (!await dbContext.Sectors.AnyAsync(cancellationToken))
        {
            await dbContext.Sectors.AddRangeAsync(
            [
                new Sector { SectorId = 1, SectorName = "Technology" },
                new Sector { SectorId = 2, SectorName = "Financials" },
                new Sector { SectorId = 3, SectorName = "Healthcare" },
                new Sector { SectorId = 4, SectorName = "Industrials" },
                new Sector { SectorId = 5, SectorName = "Energy" },
                new Sector { SectorId = 6, SectorName = "Consumer Discretionary" }
            ], cancellationToken);
        }

        if (!await dbContext.Currencies.AnyAsync(cancellationToken))
        {
            await dbContext.Currencies.AddRangeAsync(
            [
                new Currency { CurrencyId = 1, CurrencyName = "USD" },
                new Currency { CurrencyId = 2, CurrencyName = "EUR" },
                new Currency { CurrencyId = 3, CurrencyName = "GBP" },
                new Currency { CurrencyId = 4, CurrencyName = "JPY" },
                new Currency { CurrencyId = 5, CurrencyName = "SGD" },
                new Currency { CurrencyId = 6, CurrencyName = "HKD" }
            ], cancellationToken);
        }

        if (!await dbContext.Issuers.AnyAsync(cancellationToken))
        {
            await dbContext.Issuers.AddRangeAsync(
            [
                ..IssuerSeeds.Select(seed => new Issuer { IssuerId = seed.IssuerId, IssuerName = seed.IssuerName })
            ], cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Exchanges.AnyAsync(cancellationToken))
        {
            await dbContext.Exchanges.AddRangeAsync(
            [
                new Exchange { ExchangeId = 1, MicCode = "XNYS", ExchangeName = "New York Stock Exchange", Country = "United States", Timezone = "America/New_York", CurrencyId = 1 },
                new Exchange { ExchangeId = 2, MicCode = "XNAS", ExchangeName = "NASDAQ", Country = "United States", Timezone = "America/New_York", CurrencyId = 1 },
                new Exchange { ExchangeId = 3, MicCode = "XLON", ExchangeName = "London Stock Exchange", Country = "United Kingdom", Timezone = "Europe/London", CurrencyId = 3 },
                new Exchange { ExchangeId = 4, MicCode = "XETR", ExchangeName = "Deutsche Boerse Xetra", Country = "Germany", Timezone = "Europe/Berlin", CurrencyId = 2 },
                new Exchange { ExchangeId = 5, MicCode = "XTKS", ExchangeName = "Tokyo Stock Exchange", Country = "Japan", Timezone = "Asia/Tokyo", CurrencyId = 4 },
                new Exchange { ExchangeId = 6, MicCode = "XHKG", ExchangeName = "Hong Kong Stock Exchange", Country = "Hong Kong", Timezone = "Asia/Hong_Kong", CurrencyId = 6 }
            ], cancellationToken);
        }

        if (!await dbContext.IdentifierTypes.AnyAsync(cancellationToken))
        {
            await dbContext.IdentifierTypes.AddRangeAsync(
            [
                new IdentifierType { IdentifierTypeId = "ISIN", IdentifierTypeName = "ISIN", Description = "International Securities Identification Number" },
                new IdentifierType { IdentifierTypeId = "TICKER", IdentifierTypeName = "Ticker", Description = "Exchange level ticker symbol" },
                new IdentifierType { IdentifierTypeId = "CUSIP", IdentifierTypeName = "CUSIP", Description = "Committee on Uniform Securities Identification Procedures" },
                new IdentifierType { IdentifierTypeId = "SEDOL", IdentifierTypeName = "SEDOL", Description = "Stock Exchange Daily Official List" },
                new IdentifierType { IdentifierTypeId = "RIC", IdentifierTypeName = "RIC", Description = "Refinitiv Instrument Code" }
            ], cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<InstrumentIdentifier> CreateIdentifiers(
        IReadOnlyCollection<IdentifierType> identifierTypes,
        string instrumentId,
        string isin,
        DateOnly effectiveDate,
        string micCode,
        string issuerName,
        int index)
    {
        var tickerRoot = BuildTickerRoot(issuerName);
        var tickerSuffix = BuildStableToken($"{instrumentId}-ticker", 4);
        var cusip = BuildStableToken($"{instrumentId}-cusip", 9);
        var sedol = $"B{BuildStableToken($"{instrumentId}-sedol", 6)}";
        var ricSuffix = BuildStableToken($"{instrumentId}-ric", 6);

        foreach (var identifierType in identifierTypes)
        {
            yield return new InstrumentIdentifier
            {
                IdentifierId = $"ID-{identifierType.IdentifierTypeId}-{instrumentId}",
                InstrumentId = instrumentId,
                IdentifierTypeId = identifierType.IdentifierTypeId,
                IdentifierValue = identifierType.IdentifierTypeId switch
                {
                    "ISIN" => isin,
                    "TICKER" => $"{tickerRoot}{tickerSuffix}",
                    "CUSIP" => cusip,
                    "SEDOL" => sedol,
                    "RIC" => $"RIC{micCode}.{ricSuffix}",
                    _ => $"{identifierType.IdentifierTypeId}-{index + 1:0000}"
                },
                EffectiveDate = effectiveDate,
                ExpiryDate = null
            };
        }
    }

    private static string BuildInstrumentName(Random random, string issuerName, string assetClassId, string exchangeName)
    {
        var suffixes = AssetClassNameTemplates.TryGetValue(assetClassId, out var values)
            ? values
            : ["Instrument"];

        var suffix = suffixes[random.Next(suffixes.Length)];
        return $"{issuerName} {suffix} ({exchangeName})";
    }

    private static IEnumerable<InstrumentAudit> CreateAudits(Random random, string instrumentId, string status, string micCode, DateOnly updatedDate)
    {
        var auditCount = random.Next(1, 4);

        for (var sequence = 0; sequence < auditCount; sequence++)
        {
            var fieldName = AuditFields[random.Next(AuditFields.Length)];
            yield return new InstrumentAudit
            {
                AuditId = $"AUD-{instrumentId}-{sequence + 1}",
                InstrumentId = instrumentId,
                ChangedAt = updatedDate.ToDateTime(TimeOnly.MinValue).AddHours(sequence + 1),
                ChangedBy = AuditUsers[random.Next(AuditUsers.Length)],
                FieldName = fieldName,
                OldValue = fieldName switch
                {
                    "status" => "Pending",
                    "exchange_id" => "XNAS",
                    "currency_id" => "USD",
                    _ => updatedDate.AddDays(-1).ToString("yyyy-MM-dd")
                },
                NewValue = fieldName switch
                {
                    "status" => status,
                    "exchange_id" => micCode,
                    "currency_id" => "DerivedFromExchange",
                    _ => updatedDate.ToString("yyyy-MM-dd")
                },
                ChangeSource = AuditSources[random.Next(AuditSources.Length)]
            };
        }
    }

    private static string GenerateIsin(string country, string instrumentId)
    {
        var countryCode = GetCountryCode(country);
        var body = BuildStableToken($"{instrumentId}-isin", 9);
        var checkDigit = BuildStableToken($"{instrumentId}-check", 1);

        Span<char> buffer = stackalloc char[12];
        buffer[0] = countryCode[0];
        buffer[1] = countryCode[1];

        for (var position = 0; position < body.Length; position++)
        {
            buffer[position + 2] = body[position];
        }

        buffer[11] = char.IsDigit(checkDigit[0]) ? checkDigit[0] : '0';
        return new string(buffer);
    }

    private static string BuildTickerRoot(string issuerName)
    {
        var letters = issuerName.Where(char.IsLetter).Take(4).ToArray();
        if (letters.Length == 0)
        {
            return "TICK";
        }

        return new string(letters).ToUpperInvariant().PadRight(4, 'X');
    }

    private static string BuildStableToken(string value, int length)
    {
        const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const ulong offsetBasis = 14695981039346656037;
        const ulong prime = 1099511628211;

        ulong hash = offsetBasis;

        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        Span<char> buffer = stackalloc char[length];
        for (var position = length - 1; position >= 0; position--)
        {
            buffer[position] = alphabet[(int)(hash % (ulong)alphabet.Length)];
            hash /= (ulong)alphabet.Length;
        }

        return new string(buffer);
    }

    private static string GetCountryCode(string country)
    {
        return country switch
        {
            "United States" => "US",
            "United Kingdom" => "GB",
            "Germany" => "DE",
            "Japan" => "JP",
            "Hong Kong" => "HK",
            "Singapore" => "SG",
            _ => "US"
        };
    }
}

public sealed record MockDataGenerationResult(
    int InstrumentsCreated,
    int InstrumentIdentifiersCreated,
    int InstrumentAuditsCreated,
    int AssetClassesAvailable,
    int SectorsAvailable,
    int ExchangesAvailable,
    int CurrenciesAvailable,
    int IssuersAvailable,
    int IdentifierTypesAvailable,
    int? SeedUsed);