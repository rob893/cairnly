using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a named planning spendingPlan owned by a user. A spendingPlan aggregates income and expense line
/// items into a normalized summary.
/// </summary>
public sealed class SpendingPlan : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the spendingPlan ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the spendingPlan name.</summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code (e.g. "USD") for all line items.</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>Gets or sets the income line items.</summary>
    public List<SpendingPlanIncome> Incomes { get; set; } = [];

    /// <summary>Gets or sets the expense line items.</summary>
    public List<SpendingPlanExpense> Expenses { get; set; } = [];

    /// <summary>Gets or sets free-form metadata stored as jsonb for future-proofing.</summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the spendingPlan was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the spendingPlan was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the spendingPlan.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the spendingPlan.</summary>
    public int? UpdatedById { get; set; }
}