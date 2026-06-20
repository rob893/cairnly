using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for user preferences data access.
/// </summary>
public interface IUserPreferencesRepository : IRepository<UserPreferences, CursorPaginationQueryParameters>
{
    /// <summary>
    /// Gets the preferences row for a user, or <c>null</c> if none exists yet.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="track">Whether to track the entity.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The preferences, or <c>null</c>.</returns>
    Task<UserPreferences?> GetByUserIdAsync(int userId, bool track = true, CancellationToken cancellationToken = default);
}
