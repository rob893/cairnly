namespace Cairnly.API.Models.Entities;

/// <summary>
/// Join entity linking a <see cref="Entities.SpendingPlanIncome"/> to a <see cref="Entities.Tag"/>.
/// </summary>
public sealed class SpendingPlanIncomeTag : ITagLink
{
    /// <summary>Gets or sets the spendingPlan income ID.</summary>
    public int SpendingPlanIncomeId { get; set; }

    /// <summary>Gets or sets the spendingPlan income navigation property.</summary>
    public SpendingPlanIncome SpendingPlanIncome { get; set; } = default!;

    /// <summary>Gets or sets the tag ID.</summary>
    public int TagId { get; set; }

    /// <summary>Gets or sets the tag navigation property.</summary>
    public Tag Tag { get; set; } = default!;
}