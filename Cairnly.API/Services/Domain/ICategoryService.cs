using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Categories;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for category management.
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Gets a cursor-paginated list of categories visible to the current user (own + system).
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of category DTOs.</returns>
    Task<CursorPaginatedList<CategoryDto, int>> GetCategoriesAsync(CategoryQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single category by ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The category DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new category for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created category DTO.</returns>
    Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing user-owned category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated category DTO.</returns>
    Task<Result<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing user-owned category via a JSON Patch document.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated category DTO.</returns>
    Task<Result<CategoryDto>> PatchCategoryAsync(int id, JsonPatchDocument<UpdateCategoryRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a user-owned category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteCategoryAsync(int id, CancellationToken cancellationToken);
}
