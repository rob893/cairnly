using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for recording and querying per-account balance history and aggregate net worth.
/// </summary>
public interface IBalanceHistoryService
{
    /// <summary>
    /// Records today's balance snapshot for each of the given accounts. The single recording entry
    /// point: account IDs are de-duplicated, their balances are resolved in one batched query, and
    /// one snapshot is upserted per account for the current UTC day (last-write-wins). Cost is
    /// O(distinct accounts), so callers should pass the full set of affected accounts once rather
    /// than calling per transaction.
    /// </summary>
    /// <param name="accountIds">The IDs of the accounts whose balances changed.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task RecordSnapshotsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current user's net-worth history for the selected window: current totals, the change
    /// over the window (versus the earliest point in it), and the end-of-day series.
    /// </summary>
    /// <param name="queryParameters">The history query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The net-worth history.</returns>
    Task<NetWorthHistoryDto> GetNetWorthHistoryAsync(BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets per-account balance history for the current user over the selected window, used for
    /// trend sparklines and per-account change badges.
    /// </summary>
    /// <param name="queryParameters">The history query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The per-account history series.</returns>
    Task<IReadOnlyList<AccountHistoryDto>> GetAccountHistoryAsync(BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken);
}