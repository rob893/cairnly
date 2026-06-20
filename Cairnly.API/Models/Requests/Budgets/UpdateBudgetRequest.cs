using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Budgets;

/// <summary>
/// Request model for fully updating an existing budget.
/// </summary>
public sealed record UpdateBudgetRequest
{
    /// <summary>Gets the updated budget name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets an optional description.</summary>
    [MaxLength(1024)]
    public string? Description { get; init; }

    /// <summary>Gets the ISO 4217 currency code (three letters).</summary>
    [Required]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code.")]
    public string Currency { get; init; } = default!;

    /// <summary>Gets free-form metadata to associate with the budget.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateBudgetRequest"/> from an existing budget. Used as the base
    /// document for JSON Patch operations.
    /// </summary>
    /// <param name="budget">The budget entity.</param>
    /// <returns>A request reflecting the budget's current state.</returns>
    public static UpdateBudgetRequest FromEntity(Budget budget)
    {
        ArgumentNullException.ThrowIfNull(budget);

        return new UpdateBudgetRequest
        {
            Name = budget.Name,
            Description = budget.Description,
            Currency = budget.Currency,
            Metadata = budget.Metadata
        };
    }
}
