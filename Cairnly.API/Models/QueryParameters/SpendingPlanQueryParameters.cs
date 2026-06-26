namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching spendingPlans.
/// </summary>
public sealed record SpendingPlanQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }
}
