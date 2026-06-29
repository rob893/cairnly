namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// The bucket granularity for cash-flow report periods.
/// </summary>
public enum CashFlowPeriod
{
    /// <summary>Group transactions by calendar month.</summary>
    Monthly,

    /// <summary>Group transactions by calendar quarter.</summary>
    Quarterly,

    /// <summary>Group transactions by calendar year.</summary>
    Yearly
}