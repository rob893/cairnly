using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Budgets;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="BudgetService"/>.
/// </summary>
public sealed class BudgetServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<IBudgetRepository> budgetRepositoryMock;
    private readonly Mock<IBudgetIncomeRepository> incomeRepositoryMock;
    private readonly Mock<IBudgetExpenseRepository> expenseRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly BudgetService sut;

    public BudgetServiceTests()
    {
        this.budgetRepositoryMock = new Mock<IBudgetRepository>();
        this.incomeRepositoryMock = new Mock<IBudgetIncomeRepository>();
        this.expenseRepositoryMock = new Mock<IBudgetExpenseRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new BudgetService(
            NullLogger<BudgetService>.Instance,
            this.budgetRepositoryMock.Object,
            this.incomeRepositoryMock.Object,
            this.expenseRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetBudgetByIdAsync_OtherUser_ReturnsForbidden()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(1, OtherUserId));

        var result = await this.sut.GetBudgetByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateBudgetAsync_NormalizesCurrencyAndStampsAudit()
    {
        var request = new CreateBudgetRequest { Name = "Main", Currency = "usd" };

        var result = await this.sut.CreateBudgetAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.ValueOrThrow.Currency);
        this.budgetRepositoryMock.Verify(r => r.Add(It.Is<Budget>(b => b.Currency == "USD" && b.CreatedById == UserId && b.UpdatedById == UserId)), Times.Once);
        this.budgetRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBudgetSummaryAsync_NotFound_ReturnsNotFound()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Budget?)null);

        var result = await this.sut.GetBudgetSummaryAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetBudgetSummaryAsync_NormalizesAndComputesRemaining()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(1, UserId));
        this.incomeRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<BudgetIncome, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetIncome> { new() { Amount = 36500, Cadence = BudgetCadence.Annual } });
        this.expenseRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<BudgetExpense, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BudgetExpense> { new() { Amount = 3650, Cadence = BudgetCadence.Annual } });

        var result = await this.sut.GetBudgetSummaryAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summary = result.ValueOrThrow;

        // Income: annual 36500 -> daily 100, weekly 702, monthly 3042
        Assert.Equal(36500, summary.Income.Annual);
        Assert.Equal(100, summary.Income.Daily);
        Assert.Equal(702, summary.Income.Weekly);
        Assert.Equal(3042, summary.Income.Monthly);

        // Expenses: annual 3650 -> daily 10, weekly 70, monthly 304
        Assert.Equal(3650, summary.Expenses.Annual);
        Assert.Equal(10, summary.Expenses.Daily);

        // Remaining is per-column subtraction
        Assert.Equal(32850, summary.Remaining.Annual);
        Assert.Equal(90, summary.Remaining.Daily);
        Assert.Equal(632, summary.Remaining.Weekly);
        Assert.Equal(2738, summary.Remaining.Monthly);
    }

    [Fact]
    public async Task DeleteBudgetAsync_OtherUser_ReturnsForbidden()
    {
        this.budgetRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildBudget(1, OtherUserId));

        var result = await this.sut.DeleteBudgetAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.budgetRepositoryMock.Verify(r => r.Remove(It.IsAny<Budget>()), Times.Never);
    }

    private static Budget BuildBudget(int id, int userId)
    {
        return new Budget { Id = id, UserId = userId, Name = "Budget", Currency = "USD" };
    }
}
