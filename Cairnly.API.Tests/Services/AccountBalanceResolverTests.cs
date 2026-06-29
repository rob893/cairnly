using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Services.Domain;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="AccountBalanceResolver"/>.
/// </summary>
public sealed class AccountBalanceResolverTests
{
    private readonly Mock<IAccountRepository> accountRepositoryMock;
    private readonly AccountBalanceResolver sut;

    public AccountBalanceResolverTests()
    {
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.sut = new AccountBalanceResolver(this.accountRepositoryMock.Object);
    }

    [Fact]
    public async Task ResolveBalancesAsync_AddsOpeningBalanceAndTransactionSum()
    {
        var accounts = new List<Account>
        {
            new() { Id = 1, OpeningBalance = 1000 },
            new() { Id = 2, OpeningBalance = 0 }
        };

        this.accountRepositoryMock
            .Setup(r => r.GetTransactionSumsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, long> { [1] = 500 });

        var balances = await this.sut.ResolveBalancesAsync(accounts, CancellationToken.None);

        Assert.Equal(1500, balances[1]);
        Assert.Equal(0, balances[2]);
    }

    [Fact]
    public async Task ResolveBalancesAsync_EmptyAccounts_ReturnsEmpty()
    {
        var balances = await this.sut.ResolveBalancesAsync([], CancellationToken.None);

        Assert.Empty(balances);
        this.accountRepositoryMock.Verify(
            r => r.GetTransactionSumsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveBalanceAsOfAsync_AddsOpeningBalanceAndBoundedSum()
    {
        var account = new Account { Id = 1, OpeningBalance = 200 };
        var asOf = new DateOnly(2026, 6, 28);

        this.accountRepositoryMock
            .Setup(r => r.GetTransactionSumAsOfAsync(1, asOf, It.IsAny<CancellationToken>()))
            .ReturnsAsync(800);

        var balance = await this.sut.ResolveBalanceAsOfAsync(account, asOf, CancellationToken.None);

        Assert.Equal(1000, balance);
    }
}