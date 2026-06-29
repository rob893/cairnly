using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Reports;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="ReportsService"/>.
/// </summary>
public sealed class ReportsServiceTests
{
    private const int UserId = 42;

    private readonly Mock<ITransactionRepository> transactionRepositoryMock;
    private readonly Mock<IAccountRepository> accountRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly ReportsService sut;

    public ReportsServiceTests()
    {
        this.transactionRepositoryMock = new Mock<ITransactionRepository>();
        this.accountRepositoryMock = new Mock<IAccountRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);

        this.accountRepositoryMock
            .Setup(r => r.SearchAsync(It.IsAny<Expression<Func<Account, bool>>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Account> { BuildAccount("EUR") });

        this.sut = new ReportsService(
            this.transactionRepositoryMock.Object,
            this.accountRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetCashFlowAsync_SplitsIncomeAndExpensesByKindAndComputesSavingsRate()
    {
        var month = new DateTimeOffset(2026, 5, 10, 0, 0, 0, TimeSpan.Zero);
        this.SetupEntries(
            Income(month, 1000, 1, "Pay"),
            Expense(month, -400, 2, "Rent"),
            Expense(month, -100, 3, "Food"));

        var result = await this.sut.GetCashFlowAsync(new CashFlowReportQueryParameters { Timeframe = BalanceHistoryTimeframe.All }, CancellationToken.None);

        var point = Assert.Single(result.Series);
        Assert.Equal(1000, point.Income);
        Assert.Equal(500, point.Expenses);
        Assert.Equal(500, point.Net);
        Assert.Equal(1000, result.Summary.Income);
        Assert.Equal(500, result.Summary.Expenses);
        Assert.Equal(500, result.Summary.Savings);
        Assert.Equal(50d, result.Summary.SavingsRate, 3);
        Assert.Equal("May 2026", point.Label);
    }

    [Fact]
    public async Task GetCashFlowAsync_UsesAccountCurrencyOrDefaults()
    {
        this.SetupEntries();

        var result = await this.sut.GetCashFlowAsync(new CashFlowReportQueryParameters(), CancellationToken.None);

        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public async Task GetCashFlowAsync_NoIncome_SavingsRateIsZero()
    {
        var month = new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero);
        this.SetupEntries(Expense(month, -250, 2, "Rent"));

        var result = await this.sut.GetCashFlowAsync(new CashFlowReportQueryParameters(), CancellationToken.None);

        Assert.Equal(0, result.Summary.Income);
        Assert.Equal(0d, result.Summary.SavingsRate);
        Assert.Equal(-250, result.Summary.Savings);
    }

    [Fact]
    public async Task GetCashFlowAsync_GroupsByQuarterWithQuarterLabel()
    {
        this.SetupEntries(
            Income(new DateTimeOffset(2026, 1, 5, 0, 0, 0, TimeSpan.Zero), 100, 1, "Pay"),
            Income(new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero), 200, 1, "Pay"));

        var result = await this.sut.GetCashFlowAsync(
            new CashFlowReportQueryParameters { Timeframe = BalanceHistoryTimeframe.All, Period = CashFlowPeriod.Quarterly },
            CancellationToken.None);

        Assert.Equal(2, result.Series.Count);
        Assert.Equal("Q1 2026", result.Series[0].Label);
        Assert.Equal("Q2 2026", result.Series[1].Label);
    }

    [Fact]
    public async Task GetCashFlowAsync_BuildsCategoryGroupAndMerchantBreakdowns()
    {
        var month = new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero);
        this.SetupEntries(
            Expense(month, -300, categoryId: 10, merchant: "A", parentId: 99, parentName: "Bills"),
            Expense(month, -100, categoryId: 11, merchant: "A", parentId: 99, parentName: "Bills"));

        var result = await this.sut.GetCashFlowAsync(new CashFlowReportQueryParameters(), CancellationToken.None);

        var expense = Assert.Single(result.Series).ExpenseBreakdowns;
        Assert.Equal(2, expense.ByCategory.Count);
        var group = Assert.Single(expense.ByGroup);
        Assert.Equal("99", group.Key);
        Assert.Equal("Bills", group.Label);
        Assert.Equal(400, group.Amount);
        Assert.Equal(100d, group.Percent, 3);
        var merchant = Assert.Single(expense.ByMerchant);
        Assert.Equal("A", merchant.Key);
    }

    private void SetupEntries(params CashFlowEntry[] entries)
    {
        this.transactionRepositoryMock
            .Setup(r => r.GetCashFlowEntriesAsync(UserId, It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries.ToList());
    }

    private static CashFlowEntry Income(DateTimeOffset date, long amount, int categoryId, string name)
    {
        return new CashFlowEntry { Date = date, Amount = amount, Kind = CategoryKind.Income, CategoryId = categoryId, CategoryName = name };
    }

    private static CashFlowEntry Expense(DateTimeOffset date, long amount, int categoryId, string name)
    {
        return new CashFlowEntry { Date = date, Amount = amount, Kind = CategoryKind.Expense, CategoryId = categoryId, CategoryName = name };
    }

    private static CashFlowEntry Expense(DateTimeOffset date, long amount, int categoryId, string merchant, int parentId, string parentName)
    {
        return new CashFlowEntry
        {
            Date = date,
            Amount = amount,
            Kind = CategoryKind.Expense,
            CategoryId = categoryId,
            CategoryName = $"Cat {categoryId}",
            Merchant = merchant,
            ParentCategoryId = parentId,
            ParentName = parentName
        };
    }

    private static Account BuildAccount(string currency)
    {
        return new Account
        {
            Id = 1,
            UserId = UserId,
            Name = "Account",
            Type = AccountType.Checking,
            Class = AccountClass.Asset,
            Currency = currency,
            OpeningBalance = 0
        };
    }
}