namespace Cairnly.API.Models.Entities;

/// <summary>
/// Classifies the source/type of a spendingPlan income line item.
/// </summary>
public enum IncomeType
{
    /// <summary>W-2 employment income.</summary>
    W2,

    /// <summary>1099 / contractor income.</summary>
    Form1099,

    /// <summary>Self-employment / business income.</summary>
    SelfEmployment,

    /// <summary>Investment income (interest, dividends, capital gains).</summary>
    Investment,

    /// <summary>Any other income source.</summary>
    Other
}
