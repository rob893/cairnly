using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.SpendingPlanIncomes;

/// <summary>
/// Request model for fully updating an existing spendingPlan income line item.
/// </summary>
public sealed record UpdateSpendingPlanIncomeRequest
{
    /// <summary>Gets the income line name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; init; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    [Required]
    public long Amount { get; init; }

    /// <summary>Gets how often the income occurs.</summary>
    public SpendingPlanCadence Cadence { get; init; } = SpendingPlanCadence.Annual;

    /// <summary>Gets the category ID to map this income to.</summary>
    [Required]
    public int CategoryId { get; init; }

    /// <summary>Gets the IDs of tags to apply to the income line (replaces the existing set).</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets free-form metadata to associate with the income line.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateSpendingPlanIncomeRequest"/> from an existing income line. Used as the
    /// base document for JSON Patch operations.
    /// </summary>
    /// <param name="income">The income entity.</param>
    /// <returns>A request reflecting the income line's current state.</returns>
    public static UpdateSpendingPlanIncomeRequest FromEntity(SpendingPlanIncome income)
    {
        ArgumentNullException.ThrowIfNull(income);

        return new UpdateSpendingPlanIncomeRequest
        {
            Name = income.Name,
            Description = income.Description,
            Amount = income.Amount,
            Cadence = income.Cadence,
            CategoryId = income.CategoryId,
            TagIds = income.SpendingPlanIncomeTags.Select(it => it.TagId).ToList(),
            Metadata = income.Metadata
        };
    }
}