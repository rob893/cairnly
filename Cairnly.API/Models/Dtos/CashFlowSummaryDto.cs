namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Headline cash-flow totals across the selected window.
/// </summary>
public sealed record CashFlowSummaryDto
{
    /// <summary>Gets total income across the window in minor units (positive).</summary>
    public required long Income { get; init; }

    /// <summary>Gets total expenses across the window in minor units (positive magnitude).</summary>
    public required long Expenses { get; init; }

    /// <summary>Gets total savings (income minus expenses) in minor units; may be negative.</summary>
    public required long Savings { get; init; }

    /// <summary>Gets savings as a percentage of income (0 when there is no income).</summary>
    public required double SavingsRate { get; init; }
}