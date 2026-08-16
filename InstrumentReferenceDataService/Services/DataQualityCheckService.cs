using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Data;
using Microsoft.EntityFrameworkCore;
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

    // Regex for ISIN format validation, kept internal to this service.
    private static readonly Regex IsinFormatRegex = new("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled);

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
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            RunQualityCheck(dbContext).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while running the data quality check.");
        }
        
        _logger.LogInformation("Data Quality Check Service has finished its run.");
    }

    private async Task RunQualityCheck(AppDbContext dbContext)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // This logic is adapted from the InstrumentsController's quality report endpoint.
        var instruments = await dbContext.Instruments
            .AsNoTracking()
            .Select(item => new
            {
                item.InstrumentId,
                item.Name,
                item.PrimaryIsin,
                item.Status,
                item.EffectiveDate,
                item.LastUpdated,
                HasMatchingPrimaryIsinIdentifier = item.Identifiers.Any(identifier =>
                    identifier.IdentifierTypeId == "ISIN"
                    && identifier.IdentifierValue == item.PrimaryIsin
                    && identifier.EffectiveDate <= today
                    && (identifier.ExpiryDate == null || identifier.ExpiryDate >= today)),
                HasInvalidIdentifierDateRange = item.Identifiers.Any(identifier =>
                    identifier.ExpiryDate != null && identifier.ExpiryDate < identifier.EffectiveDate)
            })
            .ToListAsync();

        var reportItems = instruments
            .Select(item =>
            {
                var indicators = new List<InstrumentQualityIndicatorResponse>();

                if (string.IsNullOrWhiteSpace(item.Status))
                {
                    indicators.Add(new("STATUS_MISSING", "Instrument status is null, empty, or whitespace."));
                }

                if (!IsinFormatRegex.IsMatch(item.PrimaryIsin ?? string.Empty))
                {
                    indicators.Add(new("PRIMARY_ISIN_FORMAT_INVALID", "Primary ISIN does not match the expected 12-character ISIN format."));
                }

                if (item.EffectiveDate > item.LastUpdated)
                {
                    indicators.Add(new("EFFECTIVE_DATE_AFTER_LAST_UPDATED", "EffectiveDate is later than LastUpdated."));
                }

                if (!item.HasMatchingPrimaryIsinIdentifier)
                {
                    indicators.Add(new("PRIMARY_ISIN_IDENTIFIER_MISSING", "No active ISIN identifier exists that matches the instrument PrimaryIsin."));
                }

                if (item.HasInvalidIdentifierDateRange)
                {
                    indicators.Add(new("IDENTIFIER_DATE_RANGE_INVALID", "At least one identifier has ExpiryDate earlier than EffectiveDate."));
                }

                return new InstrumentQualityReportItemResponse(
                    item.InstrumentId,
                    item.Name,
                    item.PrimaryIsin,
                    indicators);
            })
            .Where(item => item.FailingIndicators.Any())
            .ToList();

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
