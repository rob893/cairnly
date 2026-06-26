using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.SpendingPlanIncomes;

/// <summary>
/// Request model for creating a new spendingPlan income line item.
/// </summary>
public sealed record CreateSpendingPlanIncomeRequest
{
    /// <summary>Gets the income line name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; init; }

    /// <summary>Gets the income type.</summary>
    [Required]
    public IncomeType Type { get; init; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    [Required]
    public long Amount { get; init; }

    /// <summary>Gets how often the income occurs. Defaults to annual.</summary>
    public SpendingPlanCadence Cadence { get; init; } = SpendingPlanCadence.Annual;

    /// <summary>Gets the optional category ID to map this income to.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Gets the IDs of tags to apply to the income line.</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets optional free-form metadata to associate with the income line.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
