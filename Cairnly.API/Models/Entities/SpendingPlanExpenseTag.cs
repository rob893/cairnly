namespace Cairnly.API.Models.Entities;

/// <summary>
/// Join entity linking a <see cref="Entities.SpendingPlanExpense"/> to a <see cref="Entities.Tag"/>.
/// </summary>
public sealed class SpendingPlanExpenseTag : ITagLink
{
    /// <summary>Gets or sets the spendingPlan expense ID.</summary>
    public int SpendingPlanExpenseId { get; set; }

    /// <summary>Gets or sets the spendingPlan expense navigation property.</summary>
    public SpendingPlanExpense SpendingPlanExpense { get; set; } = default!;

    /// <summary>Gets or sets the tag ID.</summary>
    public int TagId { get; set; }

    /// <summary>Gets or sets the tag navigation property.</summary>
    public Tag Tag { get; set; } = default!;
}