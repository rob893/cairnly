using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for spendingPlan expense line item data access.
/// </summary>
public sealed class SpendingPlanExpenseRepository : Repository<SpendingPlanExpense, SpendingPlanExpenseQueryParameters>, ISpendingPlanExpenseRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanExpenseRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SpendingPlanExpenseRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<SpendingPlanExpense> AddIncludes(IQueryable<SpendingPlanExpense> query)
    {
        return query.Include(e => e.SpendingPlanExpenseTags);
    }

    /// <inheritdoc />
    protected override IQueryable<SpendingPlanExpense> AddWhereClauses(IQueryable<SpendingPlanExpense> query, SpendingPlanExpenseQueryParameters searchParams)
    {
        query = query.Where(e => e.SpendingPlanId == searchParams.SpendingPlanId);

        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(e => e.UserId == searchParams.RequestingUserId);
        }

        if (searchParams.Cadence.HasValue)
        {
            query = query.Where(e => e.Cadence == searchParams.Cadence.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(e => EF.Functions.ILike(e.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}
