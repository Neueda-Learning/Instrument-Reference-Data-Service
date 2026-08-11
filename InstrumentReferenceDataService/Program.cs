using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Extensions;
using InstrumentReferenceDataService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddScoped<MockDataGenerator>();

var useSqlite = builder.Environment.IsEnvironment("Testing") || builder.Configuration.GetValue<bool>("UseSqlite");

if (useSqlite)
{
    var sqliteConnectionString = builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=instrument-reference-data.db";

    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings.DefaultConnection cannot be empty!");
    }

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
}

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsSqlite())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapGet("/", () => "Hello World!");
app.MapPost("/api/mock-data/generate", async (int? count, int? seed, MockDataGenerator generator, CancellationToken cancellationToken) =>
{
    var result = await generator.GenerateAsync(count ?? 50, seed, cancellationToken);
    return Results.Ok(result);
});

app.MapGet("/api/instruments", async (
    AppDbContext dbContext,
    string? status,
    string? assetClassId,
    int? exchangeId,
    int? issuerId,
    int skip = 0,
    int take = 50,
    CancellationToken cancellationToken = default) =>
{
    take = Math.Clamp(take, 1, 200);
    skip = Math.Max(skip, 0);

    var query = dbContext.Instruments
        .AsNoTracking()
        .Include(instrument => instrument.AssetClass)
        .Include(instrument => instrument.Sector)
        .Include(instrument => instrument.Exchange)
        .Include(instrument => instrument.Currency)
        .Include(instrument => instrument.Issuer)
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(status))
    {
        query = query.Where(instrument => instrument.Status == status);
    }

    if (!string.IsNullOrWhiteSpace(assetClassId))
    {
        query = query.Where(instrument => instrument.AssetClassId == assetClassId);
    }

    if (exchangeId.HasValue)
    {
        query = query.Where(instrument => instrument.ExchangeId == exchangeId.Value);
    }

    if (issuerId.HasValue)
    {
        query = query.Where(instrument => instrument.IssuerId == issuerId.Value);
    }

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderBy(instrument => instrument.InstrumentId)
        .Skip(skip)
        .Take(take)
        .SelectInstrumentSummary()
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        totalCount,
        skip,
        take,
        items
    });
});

app.MapGet("/api/instruments/{instrumentId}", async (string instrumentId, AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var instrument = await dbContext.Instruments
        .AsNoTracking()
        .Include(item => item.AssetClass)
        .Include(item => item.Sector)
        .Include(item => item.Exchange)
        .Include(item => item.Currency)
        .Include(item => item.Issuer)
        .Where(item => item.InstrumentId == instrumentId)
        .SelectInstrumentSummary()
        .SingleOrDefaultAsync(cancellationToken);

    if (instrument is null)
    {
        return Results.NotFound();
    }

    var identifiers = await dbContext.InstrumentIdentifiers
        .AsNoTracking()
        .Include(item => item.IdentifierType)
        .Where(item => item.InstrumentId == instrumentId)
        .OrderBy(item => item.IdentifierTypeId)
        .SelectIdentifierResponse()
        .ToListAsync(cancellationToken);

    var audits = await dbContext.InstrumentAudits
        .AsNoTracking()
        .Where(item => item.InstrumentId == instrumentId)
        .OrderByDescending(item => item.ChangedAt)
        .SelectAuditResponse()
        .ToListAsync(cancellationToken);

    return Results.Ok(new InstrumentDetailResponse(instrument, identifiers, audits));
});

app.MapGet("/api/instruments/{instrumentId}/identifiers", async (string instrumentId, AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var identifiers = await dbContext.InstrumentIdentifiers
        .AsNoTracking()
        .Include(item => item.IdentifierType)
        .Where(item => item.InstrumentId == instrumentId)
        .OrderBy(item => item.IdentifierTypeId)
        .SelectIdentifierResponse()
        .ToListAsync(cancellationToken);

    return identifiers.Count == 0 ? Results.NotFound() : Results.Ok(identifiers);
});

app.MapGet("/api/instruments/{instrumentId}/audits", async (string instrumentId, AppDbContext dbContext, CancellationToken cancellationToken) =>
{
    var audits = await dbContext.InstrumentAudits
        .AsNoTracking()
        .Where(item => item.InstrumentId == instrumentId)
        .OrderByDescending(item => item.ChangedAt)
        .SelectAuditResponse()
        .ToListAsync(cancellationToken);

    return audits.Count == 0 ? Results.NotFound() : Results.Ok(audits);
});
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
