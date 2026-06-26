using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.SpendingPlans;

/// <summary>
/// Request model for fully updating an existing spendingPlan.
/// </summary>
public sealed record UpdateSpendingPlanRequest
{
    /// <summary>Gets the updated spendingPlan name.</summary>
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

    /// <summary>Gets free-form metadata to associate with the spendingPlan.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateSpendingPlanRequest"/> from an existing spendingPlan. Used as the base
    /// document for JSON Patch operations.
    /// </summary>
    /// <param name="spendingPlan">The spendingPlan entity.</param>
    /// <returns>A request reflecting the spendingPlan's current state.</returns>
    public static UpdateSpendingPlanRequest FromEntity(SpendingPlan spendingPlan)
    {
        ArgumentNullException.ThrowIfNull(spendingPlan);

        return new UpdateSpendingPlanRequest
        {
            Name = spendingPlan.Name,
            Description = spendingPlan.Description,
            Currency = spendingPlan.Currency,
            Metadata = spendingPlan.Metadata
        };
    }
}
