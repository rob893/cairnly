namespace Cairnly.API.Models.Entities;

/// <summary>
/// Join entity linking a <see cref="Entities.BudgetIncome"/> to a <see cref="Entities.Tag"/>.
/// </summary>
public sealed class BudgetIncomeTag
{
    /// <summary>Gets or sets the budget income ID.</summary>
    public int BudgetIncomeId { get; set; }

    /// <summary>Gets or sets the budget income navigation property.</summary>
    public BudgetIncome BudgetIncome { get; set; } = default!;

    /// <summary>Gets or sets the tag ID.</summary>
    public int TagId { get; set; }

    /// <summary>Gets or sets the tag navigation property.</summary>
    public Tag Tag { get; set; } = default!;
}
