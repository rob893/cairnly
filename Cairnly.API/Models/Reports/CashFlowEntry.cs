using System;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Reports;

/// <summary>
/// A flattened, signed transaction row used to aggregate the cash-flow report. Excludes split
/// children, balance adjustments, and transfers. Amounts are signed minor units (income positive,
/// expense negative).
/// </summary>
public sealed record CashFlowEntry
{
    /// <summary>Gets when the transaction occurred.</summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>Gets the signed amount in minor units.</summary>
    public required long Amount { get; init; }

    /// <summary>Gets the kind of the transaction's category (income or expense).</summary>
    public required CategoryKind Kind { get; init; }

    /// <summary>Gets the category ID.</summary>
    public required int CategoryId { get; init; }

    /// <summary>Gets the category name.</summary>
    public required string CategoryName { get; init; }

    /// <summary>Gets the category icon, if any.</summary>
    public string? CategoryIcon { get; init; }

    /// <summary>Gets the parent (group) category ID, or null when the category is top-level.</summary>
    public int? ParentCategoryId { get; init; }

    /// <summary>Gets the parent (group) category name, or null when top-level.</summary>
    public string? ParentName { get; init; }

    /// <summary>Gets the parent (group) category icon, or null when top-level.</summary>
    public string? ParentIcon { get; init; }

    /// <summary>Gets the merchant, if any.</summary>
    public string? Merchant { get; init; }
}