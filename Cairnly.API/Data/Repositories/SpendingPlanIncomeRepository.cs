using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for spendingPlan income line item data access.
/// </summary>
public sealed class SpendingPlanIncomeRepository : SpendingPlanLineItemRepository<SpendingPlanIncome, SpendingPlanIncomeQueryParameters>, ISpendingPlanIncomeRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanIncomeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SpendingPlanIncomeRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<SpendingPlanIncome> AddIncludes(IQueryable<SpendingPlanIncome> query)
    {
        return query.Include(i => i.SpendingPlanIncomeTags);
    }

}