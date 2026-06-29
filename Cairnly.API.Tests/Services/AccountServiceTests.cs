using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Accounts;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="AccountService"/>.
/// </summary>
public sealed class AccountServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<IAccountRepository> accountRepositoryMock;
    private readonly Mock<ITransactionRepository> transactionRepositoryMock;
    private readonly Mock<ICategoryRepository> categoryRepositoryMock;
    private readonly Mock<IAccountBalanceResolver> balanceResolverMock;
    private readonly Mock<IBalanceHistoryService> balanceHistoryServiceMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly AccountService sut;

    public AccountServiceTests()
    {
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.transactionRepositoryMock = new Mock<ITransactionRepository>();
        this.categoryRepositoryMock = new Mock<ICategoryRepository>();
        this.balanceResolverMock = new Mock<IAccountBalanceResolver>();
        this.balanceHistoryServiceMock = new Mock<IBalanceHistoryService>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        // By default, resolve each account's balance to its opening balance.
        this.balanceResolverMock
            .Setup(r => r.ResolveBalancesAsync(It.IsAny<IReadOnlyList<Account>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Account> accounts, CancellationToken _) =>
                (IReadOnlyDictionary<int, long>)accounts.ToDictionary(a => a.Id, a => a.OpeningBalance));

        this.sut = new AccountService(
            NullLogger<AccountService>.Instance,
            this.accountRepositoryMock.Object,
            this.transactionRepositoryMock.Object,
            this.categoryRepositoryMock.Object,
            this.balanceResolverMock.Object,
            this.balanceHistoryServiceMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetAccountByIdAsync_NotFound_ReturnsNotFound()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await this.sut.GetAccountByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetAccountByIdAsync_OtherUser_ReturnsForbidden()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(1, OtherUserId));

        var result = await this.sut.GetAccountByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ReturnsResolvedBalance()
    {
        var account = BuildAccount(1, UserId);
        account.OpeningBalance = 1000;
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this.balanceResolverMock
            .Setup(r => r.ResolveBalancesAsync(It.IsAny<IReadOnlyList<Account>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, long> { [1] = 1500 });

        var result = await this.sut.GetAccountByIdAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1500, result.ValueOrThrow.CurrentBalance);
    }

    [Fact]
    public async Task CreateAccountAsync_NormalizesCurrencyAndPersists()
    {
        var request = new CreateAccountRequest
        {
            Name = "Checking",
            Type = AccountType.Checking,
            Class = AccountClass.Asset,
            Currency = "usd",
            OpeningBalance = 100
        };

        var result = await this.sut.CreateAccountAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.ValueOrThrow.Currency);
        Assert.Equal(UserId, result.ValueOrThrow.UserId);
        Assert.Equal(100, result.ValueOrThrow.CurrentBalance);
        this.accountRepositoryMock.Verify(r => r.Add(It.Is<Account>(a => a.Currency == "USD" && a.CreatedById == UserId && a.UpdatedById == UserId)), Times.Once);
        this.accountRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_RecordsBalanceSnapshot()
    {
        var request = new CreateAccountRequest
        {
            Name = "Checking",
            Type = AccountType.Checking,
            Class = AccountClass.Asset,
            Currency = "USD",
            OpeningBalance = 100
        };

        var result = await this.sut.CreateAccountAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        this.balanceHistoryServiceMock.Verify(
            s => s.RecordSnapshotsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_OtherUser_ReturnsForbidden()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(1, OtherUserId));

        var result = await this.sut.DeleteAccountAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.accountRepositoryMock.Verify(r => r.Remove(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task PatchAccountAsync_EmptyDocument_ReturnsValidation()
    {
        var result = await this.sut.PatchAccountAsync(1, new JsonPatchDocument<UpdateAccountRequest>(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task PatchAccountAsync_ReplaceName_UpdatesAccount()
    {
        var account = BuildAccount(1, UserId);
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var patch = new JsonPatchDocument<UpdateAccountRequest>();
        patch.Replace(a => a.Name, "Renamed");

        var result = await this.sut.PatchAccountAsync(1, patch, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.ValueOrThrow.Name);
        this.accountRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAccountAsync_OtherUser_ReturnsForbidden()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(1, OtherUserId));

        var patch = new JsonPatchDocument<UpdateAccountRequest>();
        patch.Replace(a => a.Name, "Renamed");

        var result = await this.sut.PatchAccountAsync(1, patch, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task SetBalanceAsync_CreatesAdjustmentForDeltaAndRecordsSnapshot()
    {
        var account = BuildAccount(1, UserId);
        account.OpeningBalance = 0;
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this.balanceResolverMock
            .Setup(r => r.ResolveBalanceAsOfAsync(account, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1000);
        this.categoryRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Category, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 7, Name = "Uncategorized", IsSystem = true });

        var request = new SetAccountBalanceRequest { AsOf = new DateOnly(2026, 6, 28), Balance = 1500 };

        var result = await this.sut.SetBalanceAsync(1, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        this.transactionRepositoryMock.Verify(
            r => r.Add(It.Is<Transaction>(t => t.Amount == 500 && t.IsBalanceAdjustment && t.CategoryId == 7 && t.AccountId == 1)),
            Times.Once);
        this.transactionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        this.balanceHistoryServiceMock.Verify(
            s => s.RecordSnapshotsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetBalanceAsync_NoDelta_DoesNotCreateAdjustment()
    {
        var account = BuildAccount(1, UserId);
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this.balanceResolverMock
            .Setup(r => r.ResolveBalanceAsOfAsync(account, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1500);

        var request = new SetAccountBalanceRequest { AsOf = new DateOnly(2026, 6, 28), Balance = 1500 };

        var result = await this.sut.SetBalanceAsync(1, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        this.transactionRepositoryMock.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
    }

    [Fact]
    public async Task SetBalanceAsync_OtherUser_ReturnsForbidden()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(1, OtherUserId));

        var request = new SetAccountBalanceRequest { AsOf = new DateOnly(2026, 6, 28), Balance = 1500 };

        var result = await this.sut.SetBalanceAsync(1, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.transactionRepositoryMock.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Never);
    }

    private static Account BuildAccount(int id, int userId)
    {
        return new Account
        {
            Id = id,
            UserId = userId,
            Name = "Account",
            Type = AccountType.Checking,
            Class = AccountClass.Asset,
            Currency = "USD"
        };
    }
}