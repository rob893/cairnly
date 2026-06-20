namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching tags.
/// </summary>
public sealed record TagQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }
}
