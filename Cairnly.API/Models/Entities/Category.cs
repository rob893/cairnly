using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a category used to group spending or income. Categories may be system-defined
/// (shared across all users) or user-defined.
/// </summary>
public sealed class Category : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the category ID.</summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the ID of the owning user. System categories are owned by the system user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the category name.</summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional icon (emoji) representing the category.</summary>
    [MaxLength(16)]
    public string? Icon { get; set; }

    /// <summary>Gets or sets the optional parent category ID for hierarchical categories.</summary>
    public int? ParentId { get; set; }

    /// <summary>Gets or sets the parent category navigation property.</summary>
    public Category? Parent { get; set; }

    /// <summary>Gets or sets the child categories.</summary>
    public List<Category> Children { get; set; } = [];

    /// <summary>Gets or sets the kind of money movement this category groups.</summary>
    public CategoryKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a system-defined category shared across all
    /// users.
    /// </summary>
    public bool IsSystem { get; set; }

    /// <summary>
    /// Gets or sets free-form metadata stored as jsonb for future-proofing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the category was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the category was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the category.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the category.</summary>
    public int? UpdatedById { get; set; }

    /// <summary>Gets or sets the transactions assigned to this category.</summary>
    public List<Transaction> Transactions { get; set; } = [];
}
