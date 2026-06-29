using System.Collections.Generic;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents the common mutable shape of a spendingPlan line item.
/// </summary>
public interface ISpendingPlanLineItem : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the ID of the owning user.</summary>
    new int UserId { get; set; }

    /// <summary>Gets or sets the parent spendingPlan ID.</summary>
    int SpendingPlanId { get; set; }

    /// <summary>Gets or sets the line item name.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets an optional description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the amount per cadence period, in integer minor units.</summary>
    long Amount { get; set; }

    /// <summary>Gets or sets how often the line item occurs.</summary>
    SpendingPlanCadence Cadence { get; set; }

    /// <summary>Gets or sets the category this line item maps to.</summary>
    int CategoryId { get; set; }

    /// <summary>Gets or sets free-form metadata stored as jsonb for future-proofing.</summary>
    Dictionary<string, object> Metadata { get; set; }

}