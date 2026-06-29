using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for transaction-backed financial reports.
/// </summary>
public interface IReportsService
{
    /// <summary>
    /// Builds the cash-flow report for the current user over the selected window: a per-period
    /// series of income/expense/net with per-period breakdowns, plus headline window totals.
    /// </summary>
    /// <param name="queryParameters">The report query parameters (timeframe, period).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The cash-flow report.</returns>
    Task<CashFlowReportDto> GetCashFlowAsync(CashFlowReportQueryParameters queryParameters, CancellationToken cancellationToken);
}