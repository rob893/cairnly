namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Represents shared query filters for a spendingPlan line item collection.
/// </summary>
public interface ISpendingPlanLineItemQueryParameters
{
    /// <summary>Gets or sets the parent spendingPlan ID.</summary>
    int SpendingPlanId { get; set; }

    /// <summary>Gets an optional name filter.</summary>
    string? Name { get; }
}