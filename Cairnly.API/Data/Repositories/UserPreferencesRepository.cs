using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for user preferences data access.
/// </summary>
public sealed class UserPreferencesRepository : Repository<UserPreferences, CursorPaginationQueryParameters>, IUserPreferencesRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserPreferencesRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserPreferencesRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public Task<UserPreferences?> GetByUserIdAsync(int userId, bool track = true, CancellationToken cancellationToken = default)
    {
        return this.FirstOrDefaultAsync(p => p.UserId == userId, track, cancellationToken);
    }
}