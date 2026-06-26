using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.SpendingPlanExpenses;

/// <summary>
/// Request model for fully updating an existing spendingPlan expense line item.
/// </summary>
public sealed record UpdateSpendingPlanExpenseRequest
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

    /// <summary>Gets how often the expense occurs.</summary>
    public SpendingPlanCadence Cadence { get; init; } = SpendingPlanCadence.Monthly;

    /// <summary>Gets the optional category ID to map this expense to.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Gets the IDs of tags to apply to the expense line (replaces the existing set).</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets free-form metadata to associate with the expense line.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateSpendingPlanExpenseRequest"/> from an existing expense line. Used as the
    /// base document for JSON Patch operations.
    /// </summary>
    /// <param name="expense">The expense entity.</param>
    /// <returns>A request reflecting the expense line's current state.</returns>
    public static UpdateSpendingPlanExpenseRequest FromEntity(SpendingPlanExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new UpdateSpendingPlanExpenseRequest
        {
            Name = expense.Name,
            Description = expense.Description,
            Amount = expense.Amount,
            Cadence = expense.Cadence,
            CategoryId = expense.CategoryId,
            TagIds = expense.SpendingPlanExpenseTags.Select(et => et.TagId).ToList(),
            Metadata = expense.Metadata
        };
    }
}
