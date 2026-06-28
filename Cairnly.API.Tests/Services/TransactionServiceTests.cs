using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Transactions;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="TransactionService"/>.
/// </summary>
public sealed class TransactionServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<ITransactionRepository> transactionRepositoryMock;
    private readonly Mock<IAccountRepository> accountRepositoryMock;
    private readonly Mock<ICategoryTagValidator> validatorMock;
    private readonly Mock<IBalanceHistoryService> balanceHistoryServiceMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly TransactionService sut;

    public TransactionServiceTests()
    {
        this.transactionRepositoryMock = new Mock<ITransactionRepository>();
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.validatorMock = new Mock<ICategoryTagValidator>();
        this.balanceHistoryServiceMock = new Mock<IBalanceHistoryService>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);
        this.currentUserServiceMock
            .Setup(s => s.IsUserAuthorizedForResource(It.IsAny<IOwnedByUser<int>>(), It.IsAny<bool>()))
            .Returns((IOwnedByUser<int> resource, bool _) => resource.UserId == UserId);
        this.validatorMock
            .Setup(v => v.ValidateCategoryAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        this.validatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        this.sut = new TransactionService(
            NullLogger<TransactionService>.Instance,
            this.transactionRepositoryMock.Object,
            this.accountRepositoryMock.Object,
            this.validatorMock.Object,
            this.balanceHistoryServiceMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateTransactionAsync_InvalidAccount_ReturnsValidation()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var result = await this.sut.CreateTransactionAsync(BuildCreateRequest(accountId: 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateTransactionAsync_SplitChildOnDifferentAccount_ReturnsValidation()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(10, UserId));
        this.transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(20, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTransaction(20, UserId, accountId: 11));

        var request = BuildCreateRequest(accountId: 10) with { ParentTransactionId = 20 };
        var result = await this.sut.CreateTransactionAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithValidTags_PersistsAndMapsTagIds()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(10, UserId));

        var request = BuildCreateRequest(accountId: 10) with { TagIds = new[] { 3, 4 } };
        var result = await this.sut.CreateTransactionAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { 3, 4 }, result.ValueOrThrow.TagIds);
        this.transactionRepositoryMock.Verify(r => r.Add(It.IsAny<Transaction>()), Times.Once);
        this.transactionRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        this.balanceHistoryServiceMock.Verify(
            s => s.RecordSnapshotsAsync(It.Is<IEnumerable<int>>(ids => ids.Contains(10)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateTransactionAsync_WithUnknownTag_ReturnsValidation()
    {
        this.accountRepositoryMock
            .Setup(r => r.GetByIdAsync(10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildAccount(10, UserId));
        this.validatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "One or more of the specified tags do not exist"));

        var request = BuildCreateRequest(accountId: 10) with { TagIds = new[] { 3, 4 } };
        var result = await this.sut.CreateTransactionAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_NotFound_ReturnsNotFound()
    {
        this.transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        var result = await this.sut.GetTransactionByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteTransactionAsync_OtherUser_ReturnsForbidden()
    {
        this.transactionRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTransaction(1, OtherUserId, accountId: 10));

        var result = await this.sut.DeleteTransactionAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.transactionRepositoryMock.Verify(r => r.Remove(It.IsAny<Transaction>()), Times.Never);
    }

    private static CreateTransactionRequest BuildCreateRequest(int accountId)
    {
        return new CreateTransactionRequest
        {
            AccountId = accountId,
            Date = DateTimeOffset.UtcNow,
            Amount = -500,
            Source = TransactionSource.Manual
        };
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

    private static Transaction BuildTransaction(int id, int userId, int accountId)
    {
        return new Transaction
        {
            Id = id,
            UserId = userId,
            AccountId = accountId,
            Amount = -500,
            Source = TransactionSource.Manual
        };
    }
}