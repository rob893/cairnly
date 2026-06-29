namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for net-worth and account balance history endpoints.
/// </summary>
public sealed record BalanceHistoryQueryParameters
{
    /// <summary>Gets the time window to return history for. Defaults to one month.</summary>
    public BalanceHistoryTimeframe Timeframe { get; init; } = BalanceHistoryTimeframe.OneMonth;
}