using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Tags;

/// <summary>
/// Request model for fully updating an existing tag.
/// </summary>
public sealed record UpdateTagRequest
{
    /// <summary>Gets the updated tag name.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = default!;

    /// <summary>Gets free-form metadata to associate with the tag.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateTagRequest"/> from an existing tag. Used as the base document for
    /// JSON Patch operations.
    /// </summary>
    /// <param name="tag">The tag entity.</param>
    /// <returns>A request reflecting the tag's current state.</returns>
    public static UpdateTagRequest FromEntity(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new UpdateTagRequest
        {
            Name = tag.Name,
            Metadata = tag.Metadata
        };
    }
}
