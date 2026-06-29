using System.Collections.Generic;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// An account's balance history over a selected window, used for inline trend sparklines and
/// per-account change badges.
/// </summary>
public sealed record AccountHistoryDto
{
    /// <summary>Gets the account ID.</summary>
    public required int AccountId { get; init; }

    /// <summary>Gets the ordered end-of-day balance points for the account.</summary>
    public required IReadOnlyList<AccountBalancePointDto> Points { get; init; }
}