using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for tag data access.
/// </summary>
public interface ITagRepository : IRepository<Tag, TagQueryParameters>
{
    /// <summary>
    /// Gets a tag for a user by its (case-insensitive) name.
    /// </summary>
    /// <param name="userId">The owning user ID.</param>
    /// <param name="name">The tag name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The matching tag, or <c>null</c> if none exists.</returns>
    Task<Tag?> GetByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
}