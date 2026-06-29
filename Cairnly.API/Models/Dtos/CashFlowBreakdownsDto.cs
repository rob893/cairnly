using System.Collections.Generic;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// A set of cash-flow breakdowns for one section (income or expense) grouped three ways: by
/// category, by group (parent category), and by merchant. Each list is sorted by amount descending.
/// </summary>
public sealed record CashFlowBreakdownsDto
{
    /// <summary>Gets the breakdown rows grouped by category.</summary>
    public required IReadOnlyList<CashFlowBreakdownItemDto> ByCategory { get; init; }

    /// <summary>Gets the breakdown rows grouped by group (parent category).</summary>
    public required IReadOnlyList<CashFlowBreakdownItemDto> ByGroup { get; init; }

    /// <summary>Gets the breakdown rows grouped by merchant.</summary>
    public required IReadOnlyList<CashFlowBreakdownItemDto> ByMerchant { get; init; }
}