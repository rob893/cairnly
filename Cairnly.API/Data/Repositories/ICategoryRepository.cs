using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for category data access.
/// </summary>
public interface ICategoryRepository : IRepository<Category, CategoryQueryParameters>
{
    /// <summary>
    /// Determines whether a category has any child categories (i.e. is a group rather than a
    /// leaf).
    /// </summary>
    /// <param name="categoryId">The category ID to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><c>true</c> if the category has at least one child; otherwise <c>false</c>.</returns>
    Task<bool> HasChildrenAsync(int categoryId, CancellationToken cancellationToken);
}