using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Tags;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for tag management.
/// </summary>
public interface ITagService
{
    /// <summary>
    /// Gets a cursor-paginated list of tags for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of tag DTOs.</returns>
    Task<CursorPaginatedList<TagDto, int>> GetTagsAsync(TagQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single tag by ID.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The tag DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<TagDto>> GetTagByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new tag for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created tag DTO.</returns>
    Task<Result<TagDto>> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tag DTO.</returns>
    Task<Result<TagDto>> UpdateTagAsync(int id, UpdateTagRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing tag via a JSON Patch document.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated tag DTO.</returns>
    Task<Result<TagDto>> PatchTagAsync(int id, JsonPatchDocument<UpdateTagRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a tag.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteTagAsync(int id, CancellationToken cancellationToken);
}