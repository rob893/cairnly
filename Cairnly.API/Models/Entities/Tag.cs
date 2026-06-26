using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a generic, free-form label owned by a user. Tags can be applied to any taggable
/// entity (currently transactions; spendingPlans, expenses, and other entities in the future).
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

    /// <summary>Gets or sets the ID of the user who created the tag.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the tag.</summary>
    public int? UpdatedById { get; set; }

    /// <summary>
    /// Gets or sets the transaction-tag join rows. This is the transaction-specific link; future
    /// taggable entities (e.g. spendingPlans) would add their own join collections.
    /// </summary>
    public List<TransactionTag> TransactionTags { get; set; } = [];

    /// <summary>Gets or sets the spendingPlan-income-tag join rows.</summary>
    public List<SpendingPlanIncomeTag> SpendingPlanIncomeTags { get; set; } = [];

    /// <summary>Gets or sets the spendingPlan-expense-tag join rows.</summary>
    public List<SpendingPlanExpenseTag> SpendingPlanExpenseTags { get; set; } = [];
}