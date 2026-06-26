using System;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Extensions;

/// <summary>
/// Helpers for normalizing <see cref="SpendingPlanCadence"/> values.
/// </summary>
public static class SpendingPlanCadenceExtensions
{
    /// <summary>
    /// Gets the number of times a cadence occurs per year, used to annualize spendingPlan line amounts.
    /// </summary>
    /// <param name="cadence">The cadence.</param>
    /// <returns>The number of periods per year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown cadence.</exception>
    public static int PeriodsPerYear(this SpendingPlanCadence cadence)
    {
        return cadence switch
        {
            SpendingPlanCadence.Daily => 365,
            SpendingPlanCadence.Weekly => 52,
            SpendingPlanCadence.BiWeekly => 26,
            SpendingPlanCadence.SemiMonthly => 24,
            SpendingPlanCadence.Monthly => 12,
            SpendingPlanCadence.Quarterly => 4,
            SpendingPlanCadence.Annual => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unknown spendingPlan cadence.")
        };
    }
}