using System.Collections.Generic;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A net-worth history response: the current totals, the change over the selected window, and the
/// end-of-day series driving the chart.
/// </summary>
public sealed record NetWorthHistoryDto
{
    /// <summary>Gets the ISO 4217 currency code the totals are expressed in.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the current net worth (assets minus liabilities) in minor units.</summary>
    public required long NetWorth { get; init; }

    /// <summary>Gets the current total assets in minor units.</summary>
    public required long TotalAssets { get; init; }

    /// <summary>Gets the current total liabilities in minor units.</summary>
    public required long TotalLiabilities { get; init; }

    /// <summary>
    /// Gets the change in net worth over the window, in minor units (current minus the earliest
    /// point in the window).
    /// </summary>
    public required long Change { get; init; }

    /// <summary>Gets the change in net worth over the window as a percentage of the baseline.</summary>
    public required double ChangePercent { get; init; }

    /// <summary>Gets the end-of-day net-worth series for the selected window, ordered by day.</summary>
    public required IReadOnlyList<NetWorthPointDto> Series { get; init; }
}
