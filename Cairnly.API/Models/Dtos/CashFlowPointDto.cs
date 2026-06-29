using System;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// One period bucket in a cash-flow series: income, expenses, net, and the income/expense
/// breakdowns for that period (so a slice drill-down needs no extra request).
/// </summary>
public sealed record CashFlowPointDto
{
    /// <summary>Gets the first UTC day of the period bucket.</summary>
    public required DateOnly PeriodStart { get; init; }

    /// <summary>Gets a short label for the bucket (e.g. "May 2026", "Q2 2026", "2026").</summary>
    public required string Label { get; init; }

    /// <summary>Gets total income for the period in minor units (positive).</summary>
    public required long Income { get; init; }

    /// <summary>Gets total expenses for the period in minor units (positive magnitude).</summary>
    public required long Expenses { get; init; }

    /// <summary>Gets net cash flow for the period in minor units (income minus expenses).</summary>
    public required long Net { get; init; }

    /// <summary>Gets the income breakdowns (category/group/merchant) for the period.</summary>
    public required CashFlowBreakdownsDto IncomeBreakdowns { get; init; }

    /// <summary>Gets the expense breakdowns (category/group/merchant) for the period.</summary>
    public required CashFlowBreakdownsDto ExpenseBreakdowns { get; init; }
}