using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching categories.
/// </summary>
public sealed record CategoryQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional category kind filter.</summary>
    public CategoryKind? Kind { get; init; }

    /// <summary>Gets an optional parent category ID filter.</summary>
    public int? ParentId { get; init; }

    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include shared system categories. Defaults to <c>true</c>.
    /// </summary>
    public bool IncludeSystem { get; init; } = true;
}
