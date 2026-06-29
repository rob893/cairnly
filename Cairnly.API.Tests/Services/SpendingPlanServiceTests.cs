using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.SpendingPlans;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="SpendingPlanService"/>.
/// </summary>
public sealed class SpendingPlanServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<ISpendingPlanRepository> spendingPlanRepositoryMock;
    private readonly Mock<ISpendingPlanIncomeRepository> incomeRepositoryMock;
    private readonly Mock<ISpendingPlanExpenseRepository> expenseRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly SpendingPlanService sut;

    public SpendingPlanServiceTests()
    {
        this.spendingPlanRepositoryMock = new Mock<ISpendingPlanRepository>();
        this.incomeRepositoryMock = new Mock<ISpendingPlanIncomeRepository>();
        this.expenseRepositoryMock = new Mock<ISpendingPlanExpenseRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new SpendingPlanService(
            NullLogger<SpendingPlanService>.Instance,
            this.spendingPlanRepositoryMock.Object,
            this.incomeRepositoryMock.Object,
            this.expenseRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetSpendingPlanByIdAsync_OtherUser_ReturnsForbidden()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(1, OtherUserId));

        var result = await this.sut.GetSpendingPlanByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateSpendingPlanAsync_NormalizesCurrencyAndStampsAudit()
    {
        var request = new CreateSpendingPlanRequest { Name = "Main", Currency = "usd" };

        var result = await this.sut.CreateSpendingPlanAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("USD", result.ValueOrThrow.Currency);
        this.spendingPlanRepositoryMock.Verify(r => r.Add(It.Is<SpendingPlan>(b => b.Currency == "USD" && b.CreatedById == UserId && b.UpdatedById == UserId)), Times.Once);
        this.spendingPlanRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSpendingPlanSummaryAsync_NotFound_ReturnsNotFound()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpendingPlan?)null);

        var result = await this.sut.GetSpendingPlanSummaryAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetSpendingPlanSummaryAsync_NormalizesAndComputesRemaining()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(1, UserId));
        this.incomeRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanIncome, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingPlanIncome> { new() { Amount = 36500, Cadence = SpendingPlanCadence.Annual } });
        this.expenseRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanExpense, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingPlanExpense> { new() { Amount = 3650, Cadence = SpendingPlanCadence.Annual } });

        var result = await this.sut.GetSpendingPlanSummaryAsync(1, CancellationToken.None);

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
    public async Task GetSpendingPlanSummariesAsync_ReturnsSummariesForCurrentUserPlans()
    {
        var spendingPlans = new List<SpendingPlan>
        {
            BuildSpendingPlan(1, UserId),
            BuildSpendingPlan(2, UserId),
            BuildSpendingPlan(3, OtherUserId)
        };
        var incomes = new List<SpendingPlanIncome>
        {
            new() { UserId = UserId, SpendingPlanId = 1, Amount = 12000, Cadence = SpendingPlanCadence.Annual },
            new() { UserId = UserId, SpendingPlanId = 2, Amount = 2400, Cadence = SpendingPlanCadence.Monthly },
            new() { UserId = OtherUserId, SpendingPlanId = 3, Amount = 99999, Cadence = SpendingPlanCadence.Annual }
        };
        var expenses = new List<SpendingPlanExpense>
        {
            new() { UserId = UserId, SpendingPlanId = 1, Amount = 3000, Cadence = SpendingPlanCadence.Annual },
            new() { UserId = UserId, SpendingPlanId = 2, Amount = 1200, Cadence = SpendingPlanCadence.Monthly },
            new() { UserId = OtherUserId, SpendingPlanId = 3, Amount = 99999, Cadence = SpendingPlanCadence.Annual }
        };

        this.spendingPlanRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlan, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SpendingPlan, bool>> condition, bool _, CancellationToken _) => spendingPlans.Where(condition.Compile()).ToList());
        this.incomeRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanIncome, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SpendingPlanIncome, bool>> condition, bool _, CancellationToken _) => incomes.Where(condition.Compile()).ToList());
        this.expenseRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanExpense, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SpendingPlanExpense, bool>> condition, bool _, CancellationToken _) => expenses.Where(condition.Compile()).ToList());

        var result = await this.sut.GetSpendingPlanSummariesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        var summaries = result.ValueOrThrow.OrderBy(s => s.SpendingPlanId).ToList();
        Assert.Collection(
            summaries,
            first =>
            {
                Assert.Equal(1, first.SpendingPlanId);
                Assert.Equal(12000, first.Income.Annual);
                Assert.Equal(3000, first.Expenses.Annual);
                Assert.Equal(9000, first.Remaining.Annual);
            },
            second =>
            {
                Assert.Equal(2, second.SpendingPlanId);
                Assert.Equal(28800, second.Income.Annual);
                Assert.Equal(14400, second.Expenses.Annual);
                Assert.Equal(14400, second.Remaining.Annual);
            });
    }

    [Fact]
    public async Task GetSpendingPlanSummariesAsync_NoPlans_ReturnsEmptyList()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlan, bool>>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await this.sut.GetSpendingPlanSummariesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.ValueOrThrow);
        this.incomeRepositoryMock.Verify(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanIncome, bool>>>(), false, It.IsAny<CancellationToken>()), Times.Never);
        this.expenseRepositoryMock.Verify(r => r.SearchAsync(It.IsAny<Expression<Func<SpendingPlanExpense, bool>>>(), false, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSpendingPlanAsync_OtherUser_ReturnsForbidden()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(1, OtherUserId));

        var result = await this.sut.DeleteSpendingPlanAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.spendingPlanRepositoryMock.Verify(r => r.Remove(It.IsAny<SpendingPlan>()), Times.Never);
    }

    private static SpendingPlan BuildSpendingPlan(int id, int userId)
    {
        return new SpendingPlan { Id = id, UserId = userId, Name = "SpendingPlan", Currency = "USD" };
    }
}