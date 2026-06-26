using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for tag data access.
/// </summary>
public sealed class TagRepository : Repository<Tag, TagQueryParameters>, ITagRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TagRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<Tag?> GetByNameAsync(int userId, string name, CancellationToken cancellationToken = default)
    {
        return this.Context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId && EF.Functions.ILike(t.Name, name), cancellationToken);
    }

    /// <inheritdoc />
    protected override IQueryable<Tag> AddWhereClauses(IQueryable<Tag> query, TagQueryParameters searchParams)
    {
        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(t => t.UserId == searchParams.RequestingUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(t => EF.Functions.ILike(t.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}