using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a single expense line item within a spendingPlan.
/// </summary>
public sealed class SpendingPlanExpense : ISpendingPlanLineItem
{
    /// <summary>Gets or sets the expense line ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the ID of the parent spendingPlan.</summary>
    public int SpendingPlanId { get; set; }

    /// <summary>Gets or sets the parent spendingPlan navigation property.</summary>
    public SpendingPlan SpendingPlan { get; set; } = default!;

    /// <summary>Gets or sets the expense line name.</summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the amount per cadence period, in integer minor units.</summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets how often the expense occurs.</summary>
    public SpendingPlanCadence Cadence { get; set; }

    /// <summary>Gets or sets the category this expense maps to.</summary>
    public int CategoryId { get; set; }

    /// <summary>Gets or sets the category navigation property.</summary>
    public Category Category { get; set; } = default!;

    /// <summary>Gets or sets the tag join rows linking this expense to tags.</summary>
    public List<SpendingPlanExpenseTag> SpendingPlanExpenseTags { get; set; } = [];

    /// <summary>Gets or sets free-form metadata stored as jsonb for future-proofing.</summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the expense line was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the expense line was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the expense line.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the expense line.</summary>
    public int? UpdatedById { get; set; }
}