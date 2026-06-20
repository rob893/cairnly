namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching budgets.
/// </summary>
public sealed record BudgetQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }
}
