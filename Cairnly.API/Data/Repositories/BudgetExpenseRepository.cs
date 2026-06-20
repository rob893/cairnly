using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for budget expense line item data access.
/// </summary>
public sealed class BudgetExpenseRepository : Repository<BudgetExpense, BudgetExpenseQueryParameters>, IBudgetExpenseRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetExpenseRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BudgetExpenseRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<BudgetExpense> AddIncludes(IQueryable<BudgetExpense> query)
    {
        return query.Include(e => e.BudgetExpenseTags);
    }

    /// <inheritdoc />
    protected override IQueryable<BudgetExpense> AddWhereClauses(IQueryable<BudgetExpense> query, BudgetExpenseQueryParameters searchParams)
    {
        query = query.Where(e => e.BudgetId == searchParams.BudgetId);

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
