using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching accounts.
/// </summary>
public sealed record AccountQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional account type filter.</summary>
    public AccountType? Type { get; init; }

    /// <summary>Gets an optional account class filter.</summary>
    public AccountClass? Class { get; init; }

    /// <summary>Gets an optional currency filter (exact, case-insensitive).</summary>
    public string? Currency { get; init; }

    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }
}