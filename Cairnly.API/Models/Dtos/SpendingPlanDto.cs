using System;
using System.Collections.Generic;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a spendingPlan.
/// </summary>
public sealed record SpendingPlanDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the spendingPlan ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the spendingPlan name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the ISO 4217 currency code.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets free-form metadata associated with the spendingPlan.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the spendingPlan was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the spendingPlan was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="SpendingPlanDto"/> from a <see cref="Entities.SpendingPlan"/> entity.
    /// </summary>
    /// <param name="spendingPlan">The spendingPlan entity.</param>
    /// <returns>A mapped <see cref="SpendingPlanDto"/>.</returns>
    public static SpendingPlanDto FromEntity(Entities.SpendingPlan spendingPlan)
    {
        ArgumentNullException.ThrowIfNull(spendingPlan);

        return new SpendingPlanDto
        {
            Id = spendingPlan.Id,
            UserId = spendingPlan.UserId,
            Name = spendingPlan.Name,
            Description = spendingPlan.Description,
            Currency = spendingPlan.Currency,
            Metadata = spendingPlan.Metadata,
            CreatedAt = spendingPlan.CreatedAt,
            UpdatedAt = spendingPlan.UpdatedAt
        };
    }
}
