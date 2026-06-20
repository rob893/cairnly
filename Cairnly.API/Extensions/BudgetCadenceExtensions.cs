using System;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Extensions;

/// <summary>
/// Helpers for normalizing <see cref="BudgetCadence"/> values.
/// </summary>
public static class BudgetCadenceExtensions
{
    /// <summary>
    /// Gets the number of times a cadence occurs per year, used to annualize budget line amounts.
    /// </summary>
    /// <param name="cadence">The cadence.</param>
    /// <returns>The number of periods per year.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown cadence.</exception>
    public static int PeriodsPerYear(this BudgetCadence cadence)
    {
        return cadence switch
        {
            BudgetCadence.Daily => 365,
            BudgetCadence.Weekly => 52,
            BudgetCadence.BiWeekly => 26,
            BudgetCadence.SemiMonthly => 24,
            BudgetCadence.Monthly => 12,
            BudgetCadence.Quarterly => 4,
            BudgetCadence.Annual => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(cadence), cadence, "Unknown budget cadence.")
        };
    }
}
