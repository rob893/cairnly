using System;
using System.Collections.Generic;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a spendingPlan expense line item.
/// </summary>
public sealed record SpendingPlanExpenseDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the expense line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the ID of the parent spendingPlan.</summary>
    public required int SpendingPlanId { get; init; }

    /// <summary>Gets the expense line name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    public required long Amount { get; init; }

    /// <summary>Gets how often the expense occurs.</summary>
    public required SpendingPlanCadence Cadence { get; init; }

    /// <summary>Gets the optional category this expense maps to.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Gets the IDs of the tags applied to this expense line.</summary>
    public required IReadOnlyList<int> TagIds { get; init; }

    /// <summary>Gets free-form metadata associated with the expense line.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the expense line was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the expense line was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="SpendingPlanExpenseDto"/> from a <see cref="SpendingPlanExpense"/> entity. The
    /// entity's <see cref="SpendingPlanExpense.SpendingPlanExpenseTags"/> must be loaded to populate tag IDs.
    /// </summary>
    /// <param name="expense">The expense entity.</param>
    /// <returns>A mapped <see cref="SpendingPlanExpenseDto"/>.</returns>
    public static SpendingPlanExpenseDto FromEntity(SpendingPlanExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new SpendingPlanExpenseDto
        {
            Id = expense.Id,
            UserId = expense.UserId,
            SpendingPlanId = expense.SpendingPlanId,
            Name = expense.Name,
            Description = expense.Description,
            Amount = expense.Amount,
            Cadence = expense.Cadence,
            CategoryId = expense.CategoryId,
            TagIds = expense.SpendingPlanExpenseTags.Select(et => et.TagId).ToList(),
            Metadata = expense.Metadata,
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }
}
