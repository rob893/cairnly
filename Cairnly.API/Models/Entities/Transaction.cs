using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a single money movement on an account, owned by a user.
/// </summary>
public sealed class Transaction : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the transaction ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the ID of the account this transaction belongs to.</summary>
    public int AccountId { get; set; }

    /// <summary>Gets or sets the account navigation property.</summary>
    public Account Account { get; set; } = default!;

    /// <summary>Gets or sets when the transaction occurred.</summary>
    public DateTimeOffset Date { get; set; }

    /// <summary>
    /// Gets or sets the signed amount in integer minor units. Positive for inflows, negative for
    /// outflows.
    /// </summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets the merchant the transaction was with.</summary>
    [MaxLength(255)]
    public string? Merchant { get; set; }

    /// <summary>Gets or sets an optional free-text description.</summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional category ID assigned to this transaction.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Gets or sets the category navigation property.</summary>
    public Category? Category { get; set; }

    /// <summary>Gets or sets how the transaction entered the system.</summary>
    public TransactionSource Source { get; set; }

    /// <summary>Gets or sets a value indicating whether this transaction is split into children.</summary>
    public bool IsSplit { get; set; }

    /// <summary>Gets or sets the optional parent transaction ID when this is a split child.</summary>
    public int? ParentTransactionId { get; set; }

    /// <summary>Gets or sets the parent transaction navigation property.</summary>
    public Transaction? ParentTransaction { get; set; }

    /// <summary>Gets or sets the split child transactions.</summary>
    public List<Transaction> Splits { get; set; } = [];

    /// <summary>Gets or sets the transaction-tag join rows linking this transaction to tags.</summary>
    public List<TransactionTag> TransactionTags { get; set; } = [];

    /// <summary>
    /// Gets or sets free-form metadata stored as jsonb for future-proofing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the transaction record was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the transaction record was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
