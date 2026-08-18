using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstrumentReferenceDataService.Services;

/// <summary>
/// A background service that periodically runs data quality checks on instruments.
/// The check is scheduled to run daily at 06:00 UTC.
/// </summary>
public sealed class DataQualityCheckService : IHostedService, IDisposable
{
    private readonly ILogger<DataQualityCheckService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public DataQualityCheckService(ILogger<DataQualityCheckService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data Quality Check Service is starting.");

        var now = DateTime.UtcNow;
        var nextRunTime = new DateTime(now.Year, now.Month, now.Day, 6, 0, 0, DateTimeKind.Utc);
        
        // If it's already past 6 AM today, schedule for tomorrow
        if (now > nextRunTime)
        {
            nextRunTime = nextRunTime.AddDays(1);
        }

        var initialDelay = nextRunTime - now;

        _logger.LogInformation("Next data quality check scheduled for: {NextRunTimeUtc}. Initial delay: {InitialDelay}", nextRunTime, initialDelay);

        _timer = new Timer(
            DoWork,
            null,
            initialDelay,
            TimeSpan.FromHours(24) // Repeat every 24 hours
        );

        return Task.CompletedTask;
    }

    private void DoWork(object? state)
    {
        _logger.LogInformation("Data Quality Check Service is running.");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var queryService = scope.ServiceProvider.GetRequiredService<InstrumentQueryService>();
            RunQualityCheck(queryService).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the data quality check.");
        }
        
        _logger.LogInformation("Data Quality Check Service has finished its run.");
    }

    private async Task RunQualityCheck(InstrumentQueryService queryService)
    {
        var reportItems = await queryService.GetQualityReportAsync(CancellationToken.None);

        if (reportItems.Any())
        {
            _logger.LogWarning("Data quality check found {Count} instruments with issues.", reportItems.Count);
            foreach (var item in reportItems)
            {
                var indicatorCodes = string.Join(", ", item.FailingIndicators.Select(i => i.Code));
                _logger.LogWarning("Data quality issue: InstrumentId={InstrumentId}, Name='{Name}', ISIN={PrimaryIsin}, FailingIndicators=[{Indicators}]",
                    item.InstrumentId, item.Name, item.PrimaryIsin, indicatorCodes);
            }
        }
        else
        {
            _logger.LogInformation("Data quality check completed successfully. No issues found.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Data Quality Check Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
