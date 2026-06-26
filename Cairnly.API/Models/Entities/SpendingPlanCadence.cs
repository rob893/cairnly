namespace Cairnly.API.Models.Entities;

/// <summary>
/// The recurrence cadence of a spendingPlan income or expense line item.
/// </summary>
public enum SpendingPlanCadence
{
    /// <summary>Occurs every day.</summary>
    Daily,

    /// <summary>Occurs every week.</summary>
    Weekly,

    /// <summary>Occurs every two weeks (26 times per year).</summary>
    BiWeekly,

    /// <summary>Occurs twice per month (24 times per year).</summary>
    SemiMonthly,

    /// <summary>Occurs every month.</summary>
    Monthly,

    /// <summary>Occurs every quarter (4 times per year).</summary>
    Quarterly,

    /// <summary>Occurs once per year.</summary>
    Annual
}