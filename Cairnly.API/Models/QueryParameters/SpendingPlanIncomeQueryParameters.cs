using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching a spendingPlan's income line items.
/// </summary>
public sealed record SpendingPlanIncomeQueryParameters : OwnedEntityQueryParameters, ISpendingPlanLineItemQueryParameters
{
    /// <summary>
    /// Gets or sets the parent spendingPlan ID. Set by the service from the route; not bound from the
    /// query string.
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public int SpendingPlanId { get; set; }

    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }
}