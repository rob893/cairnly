using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for budget income line item data access.
/// </summary>
public sealed class BudgetIncomeRepository : Repository<BudgetIncome, BudgetIncomeQueryParameters>, IBudgetIncomeRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetIncomeRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BudgetIncomeRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<BudgetIncome> AddIncludes(IQueryable<BudgetIncome> query)
    {
        return query.Include(i => i.BudgetIncomeTags);
    }

    /// <inheritdoc />
    protected override IQueryable<BudgetIncome> AddWhereClauses(IQueryable<BudgetIncome> query, BudgetIncomeQueryParameters searchParams)
    {
        query = query.Where(i => i.BudgetId == searchParams.BudgetId);

        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(i => i.UserId == searchParams.RequestingUserId);
        }

        if (searchParams.Type.HasValue)
        {
            query = query.Where(i => i.Type == searchParams.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(i => EF.Functions.ILike(i.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}
