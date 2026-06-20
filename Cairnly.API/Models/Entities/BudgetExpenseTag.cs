namespace Cairnly.API.Models.Entities;

/// <summary>
/// Join entity linking a <see cref="Entities.BudgetExpense"/> to a <see cref="Entities.Tag"/>.
/// </summary>
public sealed class BudgetExpenseTag : ITagLink
{
    /// <summary>Gets or sets the budget expense ID.</summary>
    public int BudgetExpenseId { get; set; }

    /// <summary>Gets or sets the budget expense navigation property.</summary>
    public BudgetExpense BudgetExpense { get; set; } = default!;

    /// <summary>Gets or sets the tag ID.</summary>
    public int TagId { get; set; }

    /// <summary>Gets or sets the tag navigation property.</summary>
    public Tag Tag { get; set; } = default!;
}
