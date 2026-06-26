namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A computed spendingPlan summary normalizing income and expenses to daily/weekly/monthly/annual columns.
/// All amounts are integer minor units in the spendingPlan's currency.
/// </summary>
public sealed record SpendingPlanSummaryDto
{
    /// <summary>Gets the spendingPlan ID.</summary>
    public required int SpendingPlanId { get; init; }

    /// <summary>Gets the spendingPlan's ISO 4217 currency code.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the total income normalized across cadence columns.</summary>
    public required CadenceAmountsDto Income { get; init; }

    /// <summary>Gets the total expenses normalized across cadence columns.</summary>
    public required CadenceAmountsDto Expenses { get; init; }

    /// <summary>Gets the remaining spendingPlan (income minus expenses) per cadence column.</summary>
    public required CadenceAmountsDto Remaining { get; init; }
}