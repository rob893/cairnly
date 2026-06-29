using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlanExpenses;
using Cairnly.API.Models.Requests.SpendingPlanIncomes;
using Cairnly.API.Services.Domain;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests that income and expense line-item implementations share the generic base flow.
/// </summary>
public sealed class SpendingPlanLineItemRefactorTests
{
    [Fact]
    public void SpendingPlanIncomeService_InheritsGenericLineItemBase()
    {
        Assert.Equal(
            typeof(SpendingPlanLineItemService<SpendingPlanIncome, SpendingPlanIncomeDto, CreateSpendingPlanIncomeRequest, UpdateSpendingPlanIncomeRequest, SpendingPlanIncomeQueryParameters, ISpendingPlanIncomeRepository>),
            typeof(SpendingPlanIncomeService).BaseType);
    }

    [Fact]
    public void SpendingPlanExpenseService_InheritsGenericLineItemBase()
    {
        Assert.Equal(
            typeof(SpendingPlanLineItemService<SpendingPlanExpense, SpendingPlanExpenseDto, CreateSpendingPlanExpenseRequest, UpdateSpendingPlanExpenseRequest, SpendingPlanExpenseQueryParameters, ISpendingPlanExpenseRepository>),
            typeof(SpendingPlanExpenseService).BaseType);
    }

    [Fact]
    public void SpendingPlanRepositories_InheritGenericLineItemBase()
    {
        Assert.Equal(
            typeof(SpendingPlanLineItemRepository<SpendingPlanIncome, SpendingPlanIncomeQueryParameters>),
            typeof(SpendingPlanIncomeRepository).BaseType);
        Assert.Equal(
            typeof(SpendingPlanLineItemRepository<SpendingPlanExpense, SpendingPlanExpenseQueryParameters>),
            typeof(SpendingPlanExpenseRepository).BaseType);
    }
}