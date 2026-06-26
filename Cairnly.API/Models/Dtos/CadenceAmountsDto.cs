namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A money amount (integer minor units) normalized across cadence columns.
/// </summary>
public sealed record CadenceAmountsDto
{
    /// <summary>Gets the daily amount in minor units.</summary>
    public required long Daily { get; init; }

    /// <summary>Gets the weekly amount in minor units.</summary>
    public required long Weekly { get; init; }

    /// <summary>Gets the monthly amount in minor units.</summary>
    public required long Monthly { get; init; }

    /// <summary>Gets the annual amount in minor units.</summary>
    public required long Annual { get; init; }
}