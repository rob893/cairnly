using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Services.Auth;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for recording and querying per-account balance history and aggregate net worth.
/// </summary>
public sealed class BalanceHistoryService : IBalanceHistoryService
{
    private const string DefaultCurrency = "USD";

    private readonly ILogger<BalanceHistoryService> logger;

    private readonly IBalanceHistoryRepository balanceHistoryRepository;

    private readonly IAccountRepository accountRepository;

    private readonly IAccountBalanceResolver balanceResolver;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BalanceHistoryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="balanceHistoryRepository">The balance history repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="balanceResolver">The account balance resolver.</param>
    /// <param name="currentUserService">The current user service.</param>
    public BalanceHistoryService(
        ILogger<BalanceHistoryService> logger,
        IBalanceHistoryRepository balanceHistoryRepository,
        IAccountRepository accountRepository,
        IAccountBalanceResolver balanceResolver,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.balanceHistoryRepository = balanceHistoryRepository ?? throw new ArgumentNullException(nameof(balanceHistoryRepository));
        this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        this.balanceResolver = balanceResolver ?? throw new ArgumentNullException(nameof(balanceResolver));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task RecordSnapshotsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accountIds);

        var ids = accountIds.Distinct().ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var accounts = await this.accountRepository.SearchAsync(a => ids.Contains(a.Id), track: false, cancellationToken);

        if (accounts.Count == 0)
        {
            return;
        }

        var balances = await this.balanceResolver.ResolveBalancesAsync(accounts, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var account in accounts)
        {
            await this.balanceHistoryRepository.UpsertSnapshotAsync(account.UserId, account.Id, today, balances[account.Id], cancellationToken);
        }

        await this.balanceHistoryRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Recorded balance snapshots for {AccountCount} account(s) on {AsOf}", accounts.Count, today);
    }

    /// <inheritdoc />
    public async Task<NetWorthHistoryDto> GetNetWorthHistoryAsync(BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var userId = this.currentUserService.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = WindowStart(queryParameters.Timeframe, today);

        var accounts = await this.accountRepository.SearchAsync(a => a.UserId == userId, track: false, cancellationToken);
        var classById = accounts.ToDictionary(a => a.Id, a => a.Class);

        var currentBalances = await this.balanceResolver.ResolveBalancesAsync(accounts, cancellationToken);
        var currency = accounts.Count > 0 ? accounts[0].Currency : DefaultCurrency;

        var (currentAssets, currentLiabilities) = AggregateTotals(currentBalances, classById);
        var currentNetWorth = currentAssets - currentLiabilities;

        var snapshots = await this.balanceHistoryRepository.GetSnapshotsForSeriesAsync(userId, from, cancellationToken);
        var series = BuildNetWorthSeries(snapshots, classById, from);

        // Ensure the series ends with an up-to-date point for today reflecting the resolved totals.
        var todayPoint = new NetWorthPointDto
        {
            AsOf = today,
            NetWorth = currentNetWorth,
            TotalAssets = currentAssets,
            TotalLiabilities = currentLiabilities
        };

        if (series.Count > 0 && series[^1].AsOf == today)
        {
            series[^1] = todayPoint;
        }
        else
        {
            series.Add(todayPoint);
        }

        var baseline = series[0];
        var change = currentNetWorth - baseline.NetWorth;
        var changePercent = baseline.NetWorth != 0
            ? (double)change / Math.Abs(baseline.NetWorth) * 100d
            : 0d;

        return new NetWorthHistoryDto
        {
            Currency = currency,
            NetWorth = currentNetWorth,
            TotalAssets = currentAssets,
            TotalLiabilities = currentLiabilities,
            Change = change,
            ChangePercent = changePercent,
            Series = series
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountHistoryDto>> GetAccountHistoryAsync(BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var userId = this.currentUserService.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = WindowStart(queryParameters.Timeframe, today);

        var snapshots = await this.balanceHistoryRepository.GetSnapshotsForSeriesAsync(userId, from, cancellationToken);

        var histories = new List<AccountHistoryDto>();

        foreach (var group in snapshots.GroupBy(s => s.AccountId))
        {
            var ordered = group.OrderBy(s => s.AsOf).ToList();
            var points = new List<AccountBalancePointDto>();

            // Collapse any pre-window baseline into a single synthetic point at the window start so
            // the earliest-in-window value (used for the change badge) is accurate.
            AccountBalanceSnapshot? baseline = null;

            foreach (var snapshot in ordered)
            {
                if (from != null && snapshot.AsOf < from.Value)
                {
                    baseline = snapshot;
                    continue;
                }

                points.Add(new AccountBalancePointDto { AsOf = snapshot.AsOf, Balance = snapshot.Balance });
            }

            if (baseline != null && from != null)
            {
                points.Insert(0, new AccountBalancePointDto { AsOf = from.Value, Balance = baseline.Balance });
            }

            if (points.Count == 0)
            {
                continue;
            }

            histories.Add(new AccountHistoryDto { AccountId = group.Key, Points = points });
        }

        return histories;
    }

    /// <summary>
    /// Computes the inclusive start day of the window for the given timeframe, or <c>null</c> for
    /// all history.
    /// </summary>
    private static DateOnly? WindowStart(BalanceHistoryTimeframe timeframe, DateOnly today)
    {
        return timeframe switch
        {
            BalanceHistoryTimeframe.OneMonth => today.AddMonths(-1),
            BalanceHistoryTimeframe.ThreeMonths => today.AddMonths(-3),
            BalanceHistoryTimeframe.OneYear => today.AddYears(-1),
            BalanceHistoryTimeframe.All => null,
            _ => today.AddMonths(-1)
        };
    }

    /// <summary>
    /// Builds the aggregate net-worth series from ordered snapshots, carrying each account's last
    /// known balance forward. Snapshots before the window seed the baseline without emitting a
    /// point; snapshots within the window emit one point per distinct day.
    /// </summary>
    private static List<NetWorthPointDto> BuildNetWorthSeries(IReadOnlyList<AccountBalanceSnapshot> snapshots, IReadOnlyDictionary<int, AccountClass> classById, DateOnly? from)
    {
        var series = new List<NetWorthPointDto>();
        var latestByAccount = new Dictionary<int, long>();

        foreach (var dayGroup in snapshots.GroupBy(s => s.AsOf).OrderBy(g => g.Key))
        {
            foreach (var snapshot in dayGroup)
            {
                latestByAccount[snapshot.AccountId] = snapshot.Balance;
            }

            if (from != null && dayGroup.Key < from.Value)
            {
                continue;
            }

            var (assets, liabilities) = AggregateTotals(latestByAccount, classById);

            series.Add(new NetWorthPointDto
            {
                AsOf = dayGroup.Key,
                NetWorth = assets - liabilities,
                TotalAssets = assets,
                TotalLiabilities = liabilities
            });
        }

        return series;
    }

    /// <summary>
    /// Splits a set of account balances into asset and liability totals (both positive magnitudes)
    /// using each account's class. Accounts missing from the class map are ignored.
    /// </summary>
    private static (long Assets, long Liabilities) AggregateTotals(IReadOnlyDictionary<int, long> balances, IReadOnlyDictionary<int, AccountClass> classById)
    {
        long assets = 0;
        long liabilities = 0;

        foreach (var (accountId, balance) in balances)
        {
            if (!classById.TryGetValue(accountId, out var accountClass))
            {
                continue;
            }

            if (accountClass == AccountClass.Asset)
            {
                assets += balance;
            }
            else
            {
                liabilities += balance;
            }
        }

        return (assets, liabilities);
    }
}