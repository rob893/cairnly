using System;
using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a category.
/// </summary>
public sealed record CategoryDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the category ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the category name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional icon (emoji) representing the category.</summary>
    public string? Icon { get; init; }

    /// <summary>Gets the optional parent category ID.</summary>
    public int? ParentId { get; init; }

    /// <summary>Gets the kind of money movement this category groups.</summary>
    public required CategoryKind Kind { get; init; }

    /// <summary>Gets a value indicating whether this is a system-defined category.</summary>
    public required bool IsSystem { get; init; }

    /// <summary>Gets free-form metadata associated with the category.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the category was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the category was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="CategoryDto"/> from a <see cref="Category"/> entity.
    /// </summary>
    /// <param name="category">The category entity.</param>
    /// <returns>A mapped <see cref="CategoryDto"/>.</returns>
    public static CategoryDto FromEntity(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryDto
        {
            Id = category.Id,
            UserId = category.UserId,
            Name = category.Name,
            Icon = category.Icon,
            ParentId = category.ParentId,
            Kind = category.Kind,
            IsSystem = category.IsSystem,
            Metadata = category.Metadata,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}