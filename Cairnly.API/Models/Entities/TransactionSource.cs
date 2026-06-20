namespace Cairnly.API.Models.Entities;

/// <summary>
/// How a transaction entered the system.
/// </summary>
public enum TransactionSource
{
    /// <summary>Entered manually by the user.</summary>
    Manual,

    /// <summary>Created from an import (e.g. CSV).</summary>
    Import
}
