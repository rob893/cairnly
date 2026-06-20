using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.BudgetIncomes;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="BudgetIncomeService"/>.
/// </summary>
public sealed class BudgetIncomeServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int BudgetId = 7;

    private readonly Mock<IBudgetIncomeRepository> incomeRepositoryMock;
    private readonly Mock<IBudgetRepository> budgetRepositoryMock;
    private readonly Mock<ICategoryTagValidator> validatorMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly BudgetIncomeService sut;

    public BudgetIncomeServiceTests()
    {
        this.incomeRepositoryMock = new Mock<IBudgetIncomeRepository>();
        this.budgetRepositoryMock = new Mock<IBudgetRepository>();
        this.validatorMock = new Mock<ICategoryTagValidator>();
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

        this.sut = new BudgetIncomeService(
            NullLogger<BudgetIncomeService>.Instance,
            this.incomeRepositoryMock.Object,
            this.budgetRepositoryMock.Object,
            this.validatorMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateIncomeAsync_BudgetNotFound_ReturnsNotFound()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var result = await this.sut.CreateIncomeAsync(BudgetId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_BudgetOwnedByOther_ReturnsForbidden()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(OtherUserId));

        var result = await this.sut.CreateIncomeAsync(BudgetId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_InvalidCategory_ReturnsValidation()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(UserId));
        this.validatorMock
            .Setup(v => v.ValidateCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "The specified category does not exist"));

        var request = BuildCreateRequest() with { CategoryId = 5 };
        var result = await this.sut.CreateIncomeAsync(BudgetId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_Valid_PersistsAndMapsTags()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(UserId));

        var request = BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateIncomeAsync(BudgetId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BudgetId, result.ValueOrThrow.BudgetId);
        Assert.Equal(new[] { 3 }, result.ValueOrThrow.TagIds);
        this.incomeRepositoryMock.Verify(r => r.Add(It.Is<BudgetIncome>(i => i.UserId == UserId && i.BudgetId == BudgetId)), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByIdAsync_WrongBudget_ReturnsNotFound()
    {
        this.incomeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetIncome { Id = 1, UserId = UserId, BudgetId = 999 });

        var result = await this.sut.GetIncomeByIdAsync(BudgetId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteIncomeAsync_OtherUser_ReturnsForbidden()
    {
        this.incomeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetIncome { Id = 1, UserId = OtherUserId, BudgetId = BudgetId });

        var result = await this.sut.DeleteIncomeAsync(BudgetId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.incomeRepositoryMock.Verify(r => r.Remove(It.IsAny<BudgetIncome>()), Times.Never);
    }

    private static CreateBudgetIncomeRequest BuildCreateRequest()
    {
        return new CreateBudgetIncomeRequest { Name = "Salary", Type = IncomeType.W2, Amount = 100000, Cadence = BudgetCadence.Annual };
    }

    private static Budget BuildBudget(int userId)
    {
        return new Budget { Id = BudgetId, UserId = userId, Name = "Budget", Currency = "USD" };
    }
}
