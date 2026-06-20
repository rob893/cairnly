using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Validates that referenced categories and tags exist and are accessible to the current user.
/// Shared by domain services whose entities may link to a category and/or tags.
/// </summary>
public interface ICategoryTagValidator
{
    /// <summary>
    /// Validates that an optional category exists and is accessible (owned by the current user, a
    /// system category, or accessed by an administrator).
    /// </summary>
    /// <param name="categoryId">The optional category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result when valid; otherwise a validation failure.</returns>
    Task<Result<bool>> ValidateCategoryAsync(int? categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that every requested tag exists and is owned by the specified user.
    /// </summary>
    /// <param name="tagIds">The distinct tag IDs to validate.</param>
    /// <param name="ownerUserId">The user expected to own the tags.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A success result when valid; otherwise a validation failure.</returns>
    Task<Result<bool>> ValidateTagsAsync(IReadOnlyCollection<int> tagIds, int ownerUserId, CancellationToken cancellationToken);
}
