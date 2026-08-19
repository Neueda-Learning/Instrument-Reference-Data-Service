using InstrumentReferenceDataService.Contracts;
using InstrumentReferenceDataService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InstrumentReferenceDataService.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed partial class InstrumentsController : ControllerBase
{
    private readonly InstrumentQueryService queryService;
    private readonly InstrumentCommandService commandService;
    private readonly ILogger<InstrumentsController> logger;

    public InstrumentsController(
        InstrumentQueryService queryService,
        InstrumentCommandService commandService,
        ILogger<InstrumentsController> logger)
    {
        this.queryService = queryService;
        this.commandService = commandService;
        this.logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InstrumentDetailResponse>> GetById(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to retrieve instrument by ID: {InstrumentId}", id);
        var instrument = await queryService.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
        {
            logger.LogWarning("Instrument with ID: {InstrumentId} not found", id);
            return NotFound();
        }
        
        logger.LogInformation("Successfully retrieved instrument with ID: {InstrumentId}", id);
        return Ok(instrument);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentDetailResponse>>> Get(
        [FromQuery] string? isin,
        [FromQuery] string? cusip,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying for instruments with ISIN: {ISIN} and CUSIP: {CUSIP}", isin, cusip);
        var instruments = await queryService.GetAsync(isin, cusip, cancellationToken);
        
        logger.LogInformation("Found {Count} instruments matching query.", instruments.Count);
        return Ok(instruments);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResultResponse<InstrumentDetailResponse>>> GetPaged(
        [FromQuery] string? isin,
        [FromQuery] string? cusip,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? sortBy = "instrumentId",
        [FromQuery] string? sortDirection = "asc",
        [FromQuery] string? freshnessFilter = null,
        [FromQuery] int staleAfterDays = 30,
        [FromQuery] int recentWithinDays = 7,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Paged query for instruments with ISIN {ISIN}, CUSIP {CUSIP}, page {PageNumber}, size {PageSize}, sort {SortBy} {SortDirection}, freshness {FreshnessFilter}",
            isin,
            cusip,
            pageNumber,
            pageSize,
            sortBy,
            sortDirection,
            freshnessFilter);

        var response = await queryService.GetPagedAsync(
            isin,
            cusip,
            pageNumber,
            pageSize,
            sortBy,
            sortDirection,
            freshnessFilter,
            staleAfterDays,
            recentWithinDays,
            cancellationToken);

        return Ok(response);
    }

    [HttpGet("monitoring")]
    public async Task<ActionResult<MonitoringDataResponse>> GetMonitoring(
        [FromQuery] int staleAfterDays = 30,
        [FromQuery] int recentWithinDays = 7,
        [FromQuery] int pageSize = 8,
        [FromQuery] int stalePageNumber = 1,
        [FromQuery] int recentPageNumber = 1,
        [FromQuery] int anomalyPageNumber = 1,
        [FromQuery] string? isin = null,
        [FromQuery] string? cusip = null,
        CancellationToken cancellationToken = default)
    {
        var response = await queryService.GetMonitoringAsync(
            staleAfterDays,
            recentWithinDays,
            pageSize,
            stalePageNumber,
            recentPageNumber,
            anomalyPageNumber,
            isin,
            cusip,
            cancellationToken);

        return Ok(response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string? id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete instrument with ID: {InstrumentId}", id);
        var deleteStatus = await commandService.DeleteAsync(id, cancellationToken);
        if (deleteStatus == DeleteInstrumentStatus.NotFound)
        {
            logger.LogWarning("Delete failed: Instrument with ID: {InstrumentId} not found.", id);
            return NotFound();
        }

        logger.LogInformation("Successfully deleted instrument with ID: {InstrumentId}", id);
        return NoContent();
    }

    [HttpGet("quality-report")]
    public async Task<ActionResult<IReadOnlyCollection<InstrumentQualityReportItemResponse>>> GetQualityReport(CancellationToken cancellationToken)
    {
        logger.LogInformation("Generating data quality report.");
        var reportItems = await queryService.GetQualityReportAsync(cancellationToken);
        
        logger.LogInformation("Data quality report generated. Found {Count} instruments with issues.", reportItems.Count);
        return Ok(reportItems);
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentDetailResponse>> Create([FromBody] CreateInstrumentRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to create instrument with ID: {InstrumentId}", request.InstrumentId);
        var createResult = await commandService.CreateAsync(request, cancellationToken);
        if (createResult.Status == CreateInstrumentStatus.BadRequest)
        {
            logger.LogWarning("Validation failed for new instrument {InstrumentId}: {Message}", request.InstrumentId, createResult.ErrorMessage);
            if (createResult.ValidationErrors is { Count: > 0 })
            {
                var validationProblem = new ValidationProblemDetails
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                };

                foreach (var error in createResult.ValidationErrors)
                {
                    validationProblem.Errors.Add(error.Key, error.Value);
                }

                return BadRequest(validationProblem);
            }

            return BadRequest(createResult.ErrorMessage);
        }

        if (createResult.Status == CreateInstrumentStatus.Conflict)
        {
            logger.LogWarning("Conflict while creating instrument {InstrumentId}: {Message}", request.InstrumentId, createResult.ErrorMessage);
            return Conflict(createResult.ErrorMessage);
        }

        logger.LogInformation("Successfully created instrument with ID {InstrumentId}", request.InstrumentId);

        var createdInstrumentId = createResult.CreatedInstrumentId ?? request.InstrumentId;
        var result = await queryService.GetByIdAsync(createdInstrumentId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createdInstrumentId }, result);
    }
}
