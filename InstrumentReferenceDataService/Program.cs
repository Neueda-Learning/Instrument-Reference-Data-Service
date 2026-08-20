using InstrumentReferenceDataService.Data;
using InstrumentReferenceDataService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();
builder.Services.AddScoped<MockDataGenerator>();
builder.Services.AddScoped<InstrumentQueryService>();
builder.Services.AddScoped<InstrumentCommandService>();
builder.Services.AddHostedService<DataQualityCheckService>();
builder.Services.AddHttpClient<InstrumentReferenceDataService.Services.GroqChatService>();
builder.Services.AddControllers();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

var enableHttpsRedirection = builder.Configuration.GetValue("EnableHttpsRedirection", !app.Environment.IsEnvironment("Testing"));

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}



app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
