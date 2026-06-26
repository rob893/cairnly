using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Categories;

/// <summary>
/// Request model for fully updating an existing category.
/// </summary>
public sealed record UpdateCategoryRequest
{
    /// <summary>Gets the updated category name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets the optional icon (emoji) representing the category.</summary>
    [MaxLength(16)]
    public string? Icon { get; init; }

    /// <summary>Gets the kind of money movement this category groups.</summary>
    [Required]
    public CategoryKind Kind { get; init; }

    /// <summary>Gets the optional parent category ID.</summary>
    public int? ParentId { get; init; }

    /// <summary>Gets free-form metadata to associate with the category.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateCategoryRequest"/> from an existing category. Used as the base
    /// document for JSON Patch operations.
    /// </summary>
    /// <param name="category">The category entity.</param>
    /// <returns>A request reflecting the category's current state.</returns>
    public static UpdateCategoryRequest FromEntity(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new UpdateCategoryRequest
        {
            Name = category.Name,
            Icon = category.Icon,
            Kind = category.Kind,
            ParentId = category.ParentId,
            Metadata = category.Metadata
        };
    }
}