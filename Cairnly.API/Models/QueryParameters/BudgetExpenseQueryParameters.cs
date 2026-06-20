using System.Text.Json.Serialization;
using Cairnly.API.Models.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching a budget's expense line items.
/// </summary>
public sealed record BudgetExpenseQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>
    /// Gets or sets the parent budget ID. Set by the service from the route; not bound from the
    /// query string.
    /// </summary>
    [BindNever]
    [JsonIgnore]
    public int BudgetId { get; set; }

    /// <summary>Gets an optional name filter (case-insensitive substring match).</summary>
    public string? Name { get; init; }

    /// <summary>Gets an optional cadence filter.</summary>
    public BudgetCadence? Cadence { get; init; }
}
