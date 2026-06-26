using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a single income line item within a spendingPlan.
/// </summary>
public sealed class SpendingPlanIncome : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the income line ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the ID of the parent spendingPlan.</summary>
    public int SpendingPlanId { get; set; }

    /// <summary>Gets or sets the parent spendingPlan navigation property.</summary>
    public SpendingPlan SpendingPlan { get; set; } = default!;

    /// <summary>Gets or sets the income line name.</summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the income type.</summary>
    public IncomeType Type { get; set; }

    /// <summary>Gets or sets the amount per cadence period, in integer minor units.</summary>
    public long Amount { get; set; }

    /// <summary>Gets or sets how often the income occurs.</summary>
    public SpendingPlanCadence Cadence { get; set; }

    /// <summary>Gets or sets the optional category this income maps to.</summary>
    public int? CategoryId { get; set; }

    /// <summary>Gets or sets the category navigation property.</summary>
    public Category? Category { get; set; }

    /// <summary>Gets or sets the tag join rows linking this income to tags.</summary>
    public List<SpendingPlanIncomeTag> SpendingPlanIncomeTags { get; set; } = [];

    /// <summary>Gets or sets free-form metadata stored as jsonb for future-proofing.</summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the income line was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the income line was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the income line.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the income line.</summary>
    public int? UpdatedById { get; set; }
}
