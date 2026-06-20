using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for budget data access.
/// </summary>
public sealed class BudgetRepository : Repository<Budget, BudgetQueryParameters>, IBudgetRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public BudgetRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<Budget> AddWhereClauses(IQueryable<Budget> query, BudgetQueryParameters searchParams)
    {
        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(b => b.UserId == searchParams.RequestingUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(b => EF.Functions.ILike(b.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}
