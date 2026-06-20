using System;
using System.Collections.Generic;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a budget.
/// </summary>
public sealed record BudgetDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the budget ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the budget name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the ISO 4217 currency code.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets free-form metadata associated with the budget.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the budget was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the budget was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="BudgetDto"/> from a <see cref="Entities.Budget"/> entity.
    /// </summary>
    /// <param name="budget">The budget entity.</param>
    /// <returns>A mapped <see cref="BudgetDto"/>.</returns>
    public static BudgetDto FromEntity(Entities.Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        return new BudgetDto
        {
            Id = budget.Id,
            UserId = budget.UserId,
            Name = budget.Name,
            Description = budget.Description,
            Currency = budget.Currency,
            Metadata = budget.Metadata,
            CreatedAt = budget.CreatedAt,
            UpdatedAt = budget.UpdatedAt
        };
    }
}
