using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a free-form label that can be applied to transactions, owned by a user.
/// </summary>
public sealed class Tag : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the tag ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the tag name.</summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets free-form metadata stored as jsonb for future-proofing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the tag was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the tag was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the transaction-tag join rows linking this tag to transactions.</summary>
    public List<TransactionTag> TransactionTags { get; set; } = [];
}
