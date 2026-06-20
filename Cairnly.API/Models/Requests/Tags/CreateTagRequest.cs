using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Requests.Tags;

/// <summary>
/// Request model for creating a new tag.
/// </summary>
public sealed record CreateTagRequest
{
    /// <summary>Gets the tag name.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = default!;

    /// <summary>Gets optional free-form metadata to associate with the tag.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
