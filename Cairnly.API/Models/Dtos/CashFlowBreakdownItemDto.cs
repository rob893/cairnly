namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A single row in a cash-flow breakdown (a category, group, or merchant) with its share of the
/// section total. Used to power the income/expense breakdown lists and bar drill-downs.
/// </summary>
public sealed record CashFlowBreakdownItemDto
{
    /// <summary>
    /// Gets the stable key for this row used when drilling into transactions. For category and group
    /// rows this is the category ID as a string; for merchant rows it is the merchant name.
    /// </summary>
    public required string Key { get; init; }

    /// <summary>Gets the human-readable label (category/group name or merchant).</summary>
    public required string Label { get; init; }

    /// <summary>Gets the optional icon (emoji) for category/group rows.</summary>
    public string? Icon { get; init; }

    /// <summary>Gets the absolute total for this row in minor units (always positive).</summary>
    public required long Amount { get; init; }

    /// <summary>Gets this row's share of the section total, as a percentage (0-100).</summary>
    public required double Percent { get; init; }
}