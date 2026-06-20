using System;
using System.Collections.Generic;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a budget income line item.
/// </summary>
public sealed record BudgetIncomeDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the income line ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the ID of the parent budget.</summary>
    public required int BudgetId { get; init; }

    /// <summary>Gets the income line name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the income type.</summary>
    public required IncomeType Type { get; init; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    public required long Amount { get; init; }

    /// <summary>Gets how often the income occurs.</summary>
    public required BudgetCadence Cadence { get; init; }

    /// <summary>Gets the optional category this income maps to.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Gets the IDs of the tags applied to this income line.</summary>
    public required IReadOnlyList<int> TagIds { get; init; }

    /// <summary>Gets free-form metadata associated with the income line.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the income line was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the income line was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="BudgetIncomeDto"/> from a <see cref="BudgetIncome"/> entity. The entity's
    /// <see cref="BudgetIncome.BudgetIncomeTags"/> must be loaded to populate tag IDs.
    /// </summary>
    /// <param name="income">The income entity.</param>
    /// <returns>A mapped <see cref="BudgetIncomeDto"/>.</returns>
    public static BudgetIncomeDto FromEntity(BudgetIncome income)
    {
        ArgumentNullException.ThrowIfNull(income);

        return new BudgetIncomeDto
        {
            Id = income.Id,
            UserId = income.UserId,
            BudgetId = income.BudgetId,
            Name = income.Name,
            Description = income.Description,
            Type = income.Type,
            Amount = income.Amount,
            Cadence = income.Cadence,
            CategoryId = income.CategoryId,
            TagIds = income.BudgetIncomeTags.Select(it => it.TagId).ToList(),
            Metadata = income.Metadata,
            CreatedAt = income.CreatedAt,
            UpdatedAt = income.UpdatedAt
        };
    }
}
