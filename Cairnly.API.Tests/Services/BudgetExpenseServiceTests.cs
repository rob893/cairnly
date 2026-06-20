using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.BudgetExpenses;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="BudgetExpenseService"/>.
/// </summary>
public sealed class BudgetExpenseServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int BudgetId = 7;

    private readonly Mock<IBudgetExpenseRepository> expenseRepositoryMock;
    private readonly Mock<IBudgetRepository> budgetRepositoryMock;
    private readonly Mock<ICategoryRepository> categoryRepositoryMock;
    private readonly Mock<ITagRepository> tagRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly BudgetExpenseService sut;

    public BudgetExpenseServiceTests()
    {
        this.expenseRepositoryMock = new Mock<IBudgetExpenseRepository>();
        this.budgetRepositoryMock = new Mock<IBudgetRepository>();
        this.categoryRepositoryMock = new Mock<ICategoryRepository>();
        this.tagRepositoryMock = new Mock<ITagRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new BudgetExpenseService(
            NullLogger<BudgetExpenseService>.Instance,
            this.expenseRepositoryMock.Object,
            this.budgetRepositoryMock.Object,
            this.categoryRepositoryMock.Object,
            this.tagRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateExpenseAsync_BudgetNotFound_ReturnsNotFound()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var result = await this.sut.CreateExpenseAsync(BudgetId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateExpenseAsync_Valid_PersistsWithCadence()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(UserId));

        var request = BuildCreateRequest() with { Cadence = BudgetCadence.Weekly };
        var result = await this.sut.CreateExpenseAsync(BudgetId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(BudgetCadence.Weekly, result.ValueOrThrow.Cadence);
        this.expenseRepositoryMock.Verify(r => r.Add(It.Is<BudgetExpense>(e => e.UserId == UserId && e.BudgetId == BudgetId)), Times.Once);
    }

    [Fact]
    public async Task CreateExpenseAsync_UnknownTag_ReturnsValidation()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(BudgetId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(UserId));
        this.tagRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<Tag, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tag>());

        var request = BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateExpenseAsync(BudgetId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task DeleteExpenseAsync_OtherUser_ReturnsForbidden()
    {
        this.expenseRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BudgetExpense { Id = 1, UserId = OtherUserId, BudgetId = BudgetId });

        var result = await this.sut.DeleteExpenseAsync(BudgetId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.expenseRepositoryMock.Verify(r => r.Remove(It.IsAny<BudgetExpense>()), Times.Never);
    }

    private static CreateBudgetExpenseRequest BuildCreateRequest()
    {
        return new CreateBudgetExpenseRequest { Name = "Rent", Amount = 200000, Cadence = BudgetCadence.Monthly };
    }

    private static Budget BuildBudget(int userId)
    {
        return new Budget { Id = BudgetId, UserId = userId, Name = "Budget", Currency = "USD" };
    }
}
