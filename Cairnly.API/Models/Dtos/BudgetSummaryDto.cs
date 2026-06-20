namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A computed budget summary normalizing income and expenses to daily/weekly/monthly/annual columns.
/// All amounts are integer minor units in the budget's currency.
/// </summary>
public sealed record BudgetSummaryDto
{
    /// <summary>Gets the budget ID.</summary>
    public required int BudgetId { get; init; }

    /// <summary>Gets the budget's ISO 4217 currency code.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the total income normalized across cadence columns.</summary>
    public required CadenceAmountsDto Income { get; init; }

    /// <summary>Gets the total expenses normalized across cadence columns.</summary>
    public required CadenceAmountsDto Expenses { get; init; }

    /// <summary>Gets the remaining budget (income minus expenses) per cadence column.</summary>
    public required CadenceAmountsDto Remaining { get; init; }
}
