using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Tags;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing tags.
/// </summary>
[Route("api/v{version:apiVersion}/tags")]
[ApiVersion("1")]
[ApiController]
public sealed class TagsController : ServiceControllerBase
{
    private readonly ITagService tagService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagsController"/> class.
    /// </summary>
    /// <param name="tagService">The tag service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public TagsController(ITagService tagService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of tags for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of tag DTOs.</returns>
    /// <response code="200">Returns the paginated list of tags.</response>
    [HttpGet(Name = nameof(GetTagsAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<TagDto>>> GetTagsAsync([FromQuery] TagQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var tags = await this.tagService.GetTagsAsync(queryParameters, cancellationToken);
        var response = tags.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific tag by ID.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The tag DTO.</returns>
    /// <response code="200">Returns the tag.</response>
    /// <response code="403">If the tag does not belong to the current user.</response>
    /// <response code="404">If the tag is not found.</response>
    [HttpGet("{id}", Name = nameof(GetTagByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagDto>> GetTagByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.tagService.GetTagByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new tag for the current user.
    /// </summary>
    /// <param name="request">The create tag request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created tag.</returns>
    /// <response code="201">The tag was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="409">If a tag with the same name already exists.</response>
    [HttpPost(Name = nameof(CreateTagAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> CreateTagAsync([FromBody] CreateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await this.tagService.CreateTagAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var tag = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetTagByIdAsync), new { id = tag.Id }, tag);
    }

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="request">The update tag request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated tag.</returns>
    /// <response code="200">Returns the updated tag.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the tag does not belong to the current user.</response>
    /// <response code="404">If the tag is not found.</response>
    /// <response code="409">If a tag with the same name already exists.</response>
    [HttpPut("{id}", Name = nameof(UpdateTagAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> UpdateTagAsync([FromRoute] int id, [FromBody] UpdateTagRequest request, CancellationToken cancellationToken)
    {
        var result = await this.tagService.UpdateTagAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing tag using a JSON Patch document.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated tag.</returns>
    /// <response code="200">Returns the updated tag.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the tag does not belong to the current user.</response>
    /// <response code="404">If the tag is not found.</response>
    /// <response code="409">If a tag with the same name already exists.</response>
    [HttpPatch("{id}", Name = nameof(PatchTagAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagDto>> PatchTagAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateTagRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.tagService.PatchTagAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes a tag.
    /// </summary>
    /// <param name="id">The tag ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The tag was deleted successfully.</response>
    /// <response code="403">If the tag does not belong to the current user.</response>
    /// <response code="404">If the tag is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteTagAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTagAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.tagService.DeleteTagAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}
