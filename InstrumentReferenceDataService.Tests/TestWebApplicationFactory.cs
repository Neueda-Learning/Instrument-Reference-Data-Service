using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InstrumentReferenceDataService.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"instrument-reference-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqliteConnection"] = $"Data Source={databasePath}"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}