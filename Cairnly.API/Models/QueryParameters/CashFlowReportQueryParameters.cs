namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for the cash-flow report endpoint.
/// </summary>
public sealed record CashFlowReportQueryParameters
{
    /// <summary>Gets the trailing time window to report on. Defaults to one year.</summary>
    public BalanceHistoryTimeframe Timeframe { get; init; } = BalanceHistoryTimeframe.OneYear;

    /// <summary>Gets the bucket granularity for periods. Defaults to monthly.</summary>
    public CashFlowPeriod Period { get; init; } = CashFlowPeriod.Monthly;
}