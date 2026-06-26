using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.SpendingPlanExpenses;

/// <summary>
/// Request model for creating a new spendingPlan expense line item.
/// </summary>
public sealed record CreateSpendingPlanExpenseRequest
{
    /// <summary>Gets the expense line name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; init; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    [Required]
    public long Amount { get; init; }

    /// <summary>Gets how often the expense occurs. Defaults to monthly.</summary>
    public SpendingPlanCadence Cadence { get; init; } = SpendingPlanCadence.Monthly;

    /// <summary>Gets the category ID to map this expense to.</summary>
    [Required]
    public int CategoryId { get; init; }

    /// <summary>Gets the IDs of tags to apply to the expense line.</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets optional free-form metadata to associate with the expense line.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}