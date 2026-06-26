using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for spendingPlan data access.
/// </summary>
public sealed class SpendingPlanRepository : Repository<SpendingPlan, SpendingPlanQueryParameters>, ISpendingPlanRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SpendingPlanRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<SpendingPlan> AddWhereClauses(IQueryable<SpendingPlan> query, SpendingPlanQueryParameters searchParams)
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
