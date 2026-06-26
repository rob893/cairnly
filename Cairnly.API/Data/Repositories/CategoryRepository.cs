using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for category data access.
/// </summary>
public sealed class CategoryRepository : Repository<Category, CategoryQueryParameters>, ICategoryRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public CategoryRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken)
    {
        return this.Context.Categories.AnyAsync(c => c.ParentId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    protected override IQueryable<Category> AddWhereClauses(IQueryable<Category> query, CategoryQueryParameters searchParams)
    {
        if (!searchParams.RequestingUserIsAdmin)
        {
            var includeSystem = searchParams.IncludeSystem;
            var userId = searchParams.RequestingUserId;

            query = query.Where(c => c.UserId == userId || (includeSystem && c.IsSystem));
        }
        else if (!searchParams.IncludeSystem)
        {
            query = query.Where(c => !c.IsSystem);
        }

        if (searchParams.Kind.HasValue)
        {
            query = query.Where(c => c.Kind == searchParams.Kind.Value);
        }

        if (searchParams.ParentId.HasValue)
        {
            query = query.Where(c => c.ParentId == searchParams.ParentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}