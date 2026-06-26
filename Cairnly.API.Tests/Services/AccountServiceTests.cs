using System.Collections.Generic;
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
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly AccountService sut;

    public AccountServiceTests()
    {
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new AccountService(
            NullLogger<AccountService>.Instance,
            this.accountRepositoryMock.Object,
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
    public async Task GetAccountByIdAsync_DerivedBalance_AddsOpeningBalanceAndTransactionSum()
    {
        var account = BuildAccount(1, UserId, isManual: false);
        account.OpeningBalance = 1000;
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        this.accountRepositoryMock
            .Setup(r => r.GetTransactionSumsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, long> { [1] = 500 });

        var result = await this.sut.GetAccountByIdAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1500, result.ValueOrThrow.CurrentBalance);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ManualBalance_ReturnsStoredBalance()
    {
        var account = BuildAccount(1, UserId, isManual: true);
        account.CurrentBalance = 2000;
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var result = await this.sut.GetAccountByIdAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000, result.ValueOrThrow.CurrentBalance);
        this.accountRepositoryMock.Verify(
            r => r.GetTransactionSumsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        this.accountRepositoryMock.Verify(r => r.Add(It.Is<Account>(a => a.Currency == "USD" && a.CreatedById == UserId && a.UpdatedById == UserId)), Times.Once);
        this.accountRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        var account = BuildAccount(1, UserId, isManual: true);
        account.CurrentBalance = 100;
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

    private static Account BuildAccount(int id, int userId, bool isManual = false)
    {
        return new Account
        {
            Id = id,
            UserId = userId,
            Name = "Account",
            Type = AccountType.Checking,
            Class = AccountClass.Asset,
            Currency = "USD",
            IsManual = isManual
        };
    }
}