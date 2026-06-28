using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for per-account daily balance snapshot data access.
/// </summary>
public interface IBalanceHistoryRepository : IRepository<AccountBalanceSnapshot, CursorPaginationQueryParameters>
{
    /// <summary>
    /// Inserts a snapshot for the given account/day, or overwrites the existing row's balance when
    /// one already exists (last-write-wins). Does not call <c>SaveChanges</c>.
    /// </summary>
    /// <param name="userId">The owning user ID.</param>
    /// <param name="accountId">The account ID.</param>
    /// <param name="asOf">The UTC day the snapshot represents.</param>
    /// <param name="balance">The resolved balance in minor units.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task UpsertSnapshotAsync(int userId, int accountId, DateOnly asOf, long balance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user's snapshots on or after <paramref name="from"/>, plus the most recent snapshot
    /// per account strictly before <paramref name="from"/> (so a series can carry the last known
    /// balance forward into the window). Ordered by <see cref="AccountBalanceSnapshot.AsOf"/>.
    /// </summary>
    /// <param name="userId">The owning user ID.</param>
    /// <param name="from">The inclusive start of the window, or <c>null</c> for all history.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The relevant snapshots ordered by day.</returns>
    Task<IReadOnlyList<AccountBalanceSnapshot>> GetSnapshotsForSeriesAsync(int userId, DateOnly? from, CancellationToken cancellationToken = default);
}
