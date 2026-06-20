using System;
using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a tag.
/// </summary>
public sealed record TagDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the tag ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the tag name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets free-form metadata associated with the tag.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the tag was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the tag was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="TagDto"/> from a <see cref="Tag"/> entity.
    /// </summary>
    /// <param name="tag">The tag entity.</param>
    /// <returns>A mapped <see cref="TagDto"/>.</returns>
    public static TagDto FromEntity(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        return new TagDto
        {
            Id = tag.Id,
            UserId = tag.UserId,
            Name = tag.Name,
            Metadata = tag.Metadata,
            CreatedAt = tag.CreatedAt,
            UpdatedAt = tag.UpdatedAt
        };
    }
}
