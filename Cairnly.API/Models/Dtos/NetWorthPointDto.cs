using System;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A single point in a net-worth time series, representing end-of-day totals for a UTC day.
/// </summary>
public sealed record NetWorthPointDto
{
    /// <summary>Gets the UTC day this point represents.</summary>
    public required DateOnly AsOf { get; init; }

    /// <summary>Gets the net worth (assets minus liabilities) in minor units.</summary>
    public required long NetWorth { get; init; }

    /// <summary>Gets the total assets in minor units.</summary>
    public required long TotalAssets { get; init; }

    /// <summary>Gets the total liabilities in minor units.</summary>
    public required long TotalLiabilities { get; init; }
}