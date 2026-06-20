namespace Cairnly.API.Models.Entities;

/// <summary>
/// The kind of money movement a category groups.
/// </summary>
public enum CategoryKind
{
    /// <summary>Money coming in.</summary>
    Income,

    /// <summary>Money going out.</summary>
    Expense,

    /// <summary>A movement between accounts (not spend or income).</summary>
    Transfer
}
