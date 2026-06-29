using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests;

/// <summary>
/// Represents the common request shape for creating or updating a spendingPlan line item.
/// </summary>
public interface ISpendingPlanLineItemRequest
{
    /// <summary>Gets the line item name.</summary>
    string Name { get; }

    /// <summary>Gets an optional description.</summary>
    string? Description { get; }

    /// <summary>Gets the amount per cadence period, in integer minor units.</summary>
    long Amount { get; }

    /// <summary>Gets how often the line item occurs.</summary>
    SpendingPlanCadence Cadence { get; }

    /// <summary>Gets the category ID to map this line item to.</summary>
    int CategoryId { get; }

    /// <summary>Gets the IDs of tags to apply to the line item.</summary>
    IReadOnlyList<int>? TagIds { get; }

    /// <summary>Gets optional free-form metadata to associate with the line item.</summary>
    Dictionary<string, object>? Metadata { get; }
}