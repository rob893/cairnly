using System;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A single point in an account's balance history, representing its end-of-day balance for a day.
/// </summary>
public sealed record AccountBalancePointDto
{
    /// <summary>Gets the UTC day this point represents.</summary>
    public required DateOnly AsOf { get; init; }

    /// <summary>Gets the account's balance in minor units for the day.</summary>
    public required long Balance { get; init; }
}
