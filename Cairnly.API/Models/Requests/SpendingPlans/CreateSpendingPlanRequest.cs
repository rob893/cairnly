using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Requests.SpendingPlans;

/// <summary>
/// Request model for creating a new spendingPlan.
/// </summary>
public sealed record CreateSpendingPlanRequest
{
    /// <summary>Gets the spendingPlan name.</summary>
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

    /// <summary>Gets optional free-form metadata to associate with the spendingPlan.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
