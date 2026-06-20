namespace Cairnly.API.Models.Entities;

/// <summary>
/// Whether an account represents an asset or a liability.
/// </summary>
public enum AccountClass
{
    /// <summary>An asset (positive net worth contribution).</summary>
    Asset,

    /// <summary>A liability (negative net worth contribution).</summary>
    Liability
}
