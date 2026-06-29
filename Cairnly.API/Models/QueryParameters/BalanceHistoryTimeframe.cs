namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// The time window for net-worth and account balance history queries.
/// </summary>
public enum BalanceHistoryTimeframe
{
    /// <summary>The trailing one month.</summary>
    OneMonth,

    /// <summary>The trailing three months.</summary>
    ThreeMonths,

    /// <summary>The trailing one year.</summary>
    OneYear,

    /// <summary>All available history.</summary>
    All
}