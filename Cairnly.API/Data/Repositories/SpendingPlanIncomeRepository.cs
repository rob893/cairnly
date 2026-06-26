using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for spendingPlan income line item data access.
/// </summary>
public sealed class SpendingPlanIncomeRepository : Repository<SpendingPlanIncome, SpendingPlanIncomeQueryParameters>, ISpendingPlanIncomeRepository
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

    /// <inheritdoc />
    protected override IQueryable<SpendingPlanIncome> AddWhereClauses(IQueryable<SpendingPlanIncome> query, SpendingPlanIncomeQueryParameters searchParams)
    {
        query = query.Where(i => i.SpendingPlanId == searchParams.SpendingPlanId);

        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(i => i.UserId == searchParams.RequestingUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}