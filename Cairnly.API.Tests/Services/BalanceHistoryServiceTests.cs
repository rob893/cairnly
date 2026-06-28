using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="BalanceHistoryService"/>.
/// </summary>
public sealed class BalanceHistoryServiceTests
{
    private const int UserId = 42;

    private readonly Mock<IBalanceHistoryRepository> balanceHistoryRepositoryMock;
    private readonly Mock<IAccountRepository> accountRepositoryMock;
    private readonly Mock<IAccountBalanceResolver> balanceResolverMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly BalanceHistoryService sut;

    public BalanceHistoryServiceTests()
    {
        this.balanceHistoryRepositoryMock = new Mock<IBalanceHistoryRepository>();
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.balanceResolverMock = new Mock<IAccountBalanceResolver>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);

        // By default, resolve each account's balance to its opening balance.
        this.balanceResolverMock
            .Setup(r => r.ResolveBalancesAsync(It.IsAny<IReadOnlyList<Account>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Account> accounts, CancellationToken _) =>
                (IReadOnlyDictionary<int, long>)accounts.ToDictionary(a => a.Id, a => a.OpeningBalance));

        this.sut = new BalanceHistoryService(
            NullLogger<BalanceHistoryService>.Instance,
            this.balanceHistoryRepositoryMock.Object,
            this.accountRepositoryMock.Object,
            this.balanceResolverMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task RecordSnapshotsAsync_EmptyIds_DoesNothing()
    {
        await this.sut.RecordSnapshotsAsync([], CancellationToken.None);

        this.accountRepositoryMock.Verify(
            r => r.SearchAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        this.balanceHistoryRepositoryMock.Verify(
            r => r.UpsertSnapshotAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordSnapshotsAsync_DedupesAndUpsertsResolvedBalancePerAccount()
    {
        var first = BuildAccount(1, openingBalance: 500);
        var second = BuildAccount(2, openingBalance: 100);

        this.SetupAccounts(first, second);
        this.balanceResolverMock
            .Setup(r => r.ResolveBalancesAsync(It.IsAny<IReadOnlyList<Account>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, long> { [1] = 500, [2] = 150 });

        await this.sut.RecordSnapshotsAsync([1, 1, 2], CancellationToken.None);

        this.balanceHistoryRepositoryMock.Verify(
            r => r.UpsertSnapshotAsync(UserId, 1, It.IsAny<DateOnly>(), 500, It.IsAny<CancellationToken>()),
            Times.Once);
        this.balanceHistoryRepositoryMock.Verify(
            r => r.UpsertSnapshotAsync(UserId, 2, It.IsAny<DateOnly>(), 150, It.IsAny<CancellationToken>()),
            Times.Once);
        this.balanceHistoryRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNetWorthHistoryAsync_ComputesTotalsAndChangeVersusEarliestPoint()
    {
        var asset = BuildAccount(1, openingBalance: 1000, accountClass: AccountClass.Asset);
        var liability = BuildAccount(2, openingBalance: 300, accountClass: AccountClass.Liability);
        this.SetupAccounts(asset, liability);

        var earlier = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-5);
        this.balanceHistoryRepositoryMock
            .Setup(r => r.GetSnapshotsForSeriesAsync(UserId, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountBalanceSnapshot>
            {
                BuildSnapshot(1, earlier, 900),
                BuildSnapshot(2, earlier, 300)
            });

        var result = await this.sut.GetNetWorthHistoryAsync(new BalanceHistoryQueryParameters { Timeframe = BalanceHistoryTimeframe.All }, CancellationToken.None);

        Assert.Equal("USD", result.Currency);
        Assert.Equal(1000, result.TotalAssets);
        Assert.Equal(300, result.TotalLiabilities);
        Assert.Equal(700, result.NetWorth);
        // Baseline net worth was 900 - 300 = 600; current is 700.
        Assert.Equal(100, result.Change);
        Assert.Equal(100d / 600d * 100d, result.ChangePercent, 3);
        Assert.Equal(2, result.Series.Count);
        Assert.Equal(700, result.Series[^1].NetWorth);
    }

    [Fact]
    public async Task GetNetWorthHistoryAsync_NoSnapshots_ReturnsSingleCurrentPointWithZeroChange()
    {
        var asset = BuildAccount(1, openingBalance: 500, accountClass: AccountClass.Asset);
        this.SetupAccounts(asset);

        this.balanceHistoryRepositoryMock
            .Setup(r => r.GetSnapshotsForSeriesAsync(UserId, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountBalanceSnapshot>());

        var result = await this.sut.GetNetWorthHistoryAsync(new BalanceHistoryQueryParameters(), CancellationToken.None);

        Assert.Equal(500, result.NetWorth);
        Assert.Equal(0, result.Change);
        Assert.Equal(0d, result.ChangePercent);
        Assert.Single(result.Series);
    }

    [Fact]
    public async Task GetAccountHistoryAsync_CollapsesPreWindowBaselineToWindowStart()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowStart = today.AddMonths(-1);

        this.balanceHistoryRepositoryMock
            .Setup(r => r.GetSnapshotsForSeriesAsync(UserId, It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountBalanceSnapshot>
            {
                BuildSnapshot(1, windowStart.AddDays(-5), 100),
                BuildSnapshot(1, today, 200)
            });

        var result = await this.sut.GetAccountHistoryAsync(new BalanceHistoryQueryParameters { Timeframe = BalanceHistoryTimeframe.OneMonth }, CancellationToken.None);

        var history = Assert.Single(result);
        Assert.Equal(1, history.AccountId);
        Assert.Equal(2, history.Points.Count);
        Assert.Equal(windowStart, history.Points[0].AsOf);
        Assert.Equal(100, history.Points[0].Balance);
        Assert.Equal(200, history.Points[1].Balance);
    }

    private void SetupAccounts(params Account[] accounts)
    {
        this.accountRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts.ToList());
    }

    private static Account BuildAccount(int id, long openingBalance = 0, AccountClass accountClass = AccountClass.Asset)
    {
        return new Account
        {
            Id = id,
            UserId = UserId,
            Name = $"Account {id}",
            Type = AccountType.Checking,
            Class = accountClass,
            Currency = "USD",
            OpeningBalance = openingBalance
        };
    }

    private static AccountBalanceSnapshot BuildSnapshot(int accountId, DateOnly asOf, long balance)
    {
        return new AccountBalanceSnapshot
        {
            UserId = UserId,
            AccountId = accountId,
            AsOf = asOf,
            Balance = balance
        };
    }
}
