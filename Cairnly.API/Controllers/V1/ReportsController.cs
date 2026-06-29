using System;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for transaction-backed financial reports.
/// </summary>
[Route("api/v{version:apiVersion}/reports")]
[ApiVersion("1")]
[ApiController]
public sealed class ReportsController : ServiceControllerBase
{
    private readonly IReportsService reportsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsController"/> class.
    /// </summary>
    /// <param name="reportsService">The reports service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public ReportsController(IReportsService reportsService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.reportsService = reportsService ?? throw new ArgumentNullException(nameof(reportsService));
    }

    /// <summary>
    /// Gets the current user's cash-flow report for the selected window and bucket granularity.
    /// </summary>
    /// <param name="queryParameters">The report query parameters (timeframe, period).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The cash-flow report.</returns>
    /// <response code="200">Returns the cash-flow report.</response>
    [HttpGet("cashflow", Name = nameof(GetCashFlowAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CashFlowReportDto>> GetCashFlowAsync([FromQuery] CashFlowReportQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var report = await this.reportsService.GetCashFlowAsync(queryParameters, cancellationToken);

        return this.Ok(report);
    }
}