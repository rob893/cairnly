using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Categories;

/// <summary>
/// Request model for creating a new category.
/// </summary>
public sealed record CreateCategoryRequest
{
    /// <summary>Gets the category name.</summary>
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

    /// <summary>Gets optional free-form metadata to associate with the category.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}