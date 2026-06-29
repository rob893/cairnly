using System.Collections.Generic;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A cash-flow report: the currency, the bucket granularity, a window summary, and the per-period
/// series (each period carrying its own income/expense breakdowns).
/// </summary>
public sealed record CashFlowReportDto
{
    /// <summary>Gets the ISO 4217 currency code the amounts are expressed in.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the bucket granularity the series is grouped by.</summary>
    public required CashFlowPeriod Period { get; init; }

    /// <summary>Gets the headline totals across the whole window.</summary>
    public required CashFlowSummaryDto Summary { get; init; }

    /// <summary>Gets the per-period series, ordered oldest to newest.</summary>
    public required IReadOnlyList<CashFlowPointDto> Series { get; init; }
}