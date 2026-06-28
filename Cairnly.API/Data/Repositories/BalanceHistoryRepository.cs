using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for per-account daily balance snapshot data access.
/// </summary>
public sealed class BalanceHistoryRepository : Repository<AccountBalanceSnapshot, CursorPaginationQueryParameters>, IBalanceHistoryRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BalanceHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BalanceHistoryRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task UpsertSnapshotAsync(int userId, int accountId, DateOnly asOf, long balance, CancellationToken cancellationToken = default)
    {
        var existing = await this.Context.AccountBalanceSnapshots
            .FirstOrDefaultAsync(s => s.AccountId == accountId && s.AsOf == asOf, cancellationToken);

        if (existing != null)
        {
            existing.Balance = balance;
            return;
        }

        this.Context.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
        {
            UserId = userId,
            AccountId = accountId,
            AsOf = asOf,
            Balance = balance
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountBalanceSnapshot>> GetSnapshotsForSeriesAsync(int userId, DateOnly? from, CancellationToken cancellationToken = default)
    {
        if (from == null)
        {
            return await this.Context.AccountBalanceSnapshots
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.AsOf)
                .ToListAsync(cancellationToken);
        }

        var windowStart = from.Value;

        var inWindow = await this.Context.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.AsOf >= windowStart)
            .ToListAsync(cancellationToken);

        // The most recent snapshot per account strictly before the window, so the series can carry
        // each account's last known balance forward to the window's start.
        var priorBaselines = await this.Context.AccountBalanceSnapshots
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.AsOf < windowStart)
            .GroupBy(s => s.AccountId)
            .Select(g => g.OrderByDescending(s => s.AsOf).First())
            .ToListAsync(cancellationToken);

        return inWindow
            .Concat(priorBaselines)
            .OrderBy(s => s.AsOf)
            .ToList();
    }
}
