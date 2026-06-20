using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Categories;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing categories.
/// </summary>
[Route("api/v{version:apiVersion}/categories")]
[ApiVersion("1")]
[ApiController]
public sealed class CategoriesController : ServiceControllerBase
{
    private readonly ICategoryService categoryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoriesController"/> class.
    /// </summary>
    /// <param name="categoryService">The category service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public CategoriesController(ICategoryService categoryService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of categories visible to the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of category DTOs.</returns>
    /// <response code="200">Returns the paginated list of categories.</response>
    [HttpGet(Name = nameof(GetCategoriesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<CategoryDto>>> GetCategoriesAsync([FromQuery] CategoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var categories = await this.categoryService.GetCategoriesAsync(queryParameters, cancellationToken);
        var response = categories.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific category by ID.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The category DTO.</returns>
    /// <response code="200">Returns the category.</response>
    /// <response code="403">If the category is not accessible to the current user.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpGet("{id}", Name = nameof(GetCategoryByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetCategoryByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.categoryService.GetCategoryByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new category for the current user.
    /// </summary>
    /// <param name="request">The create category request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created category.</returns>
    /// <response code="201">The category was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost(Name = nameof(CreateCategoryAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategoryAsync([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await this.categoryService.CreateCategoryAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var category = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetCategoryByIdAsync), new { id = category.Id }, category);
    }

    /// <summary>
    /// Updates an existing category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="request">The update category request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the category is not modifiable by the current user.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpPut("{id}", Name = nameof(UpdateCategoryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategoryAsync([FromRoute] int id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await this.categoryService.UpdateCategoryAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing category using a JSON Patch document.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated category.</returns>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the category is not modifiable by the current user.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpPatch("{id}", Name = nameof(PatchCategoryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> PatchCategoryAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateCategoryRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.categoryService.PatchCategoryAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes a category.
    /// </summary>
    /// <param name="id">The category ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The category was deleted successfully.</response>
    /// <response code="403">If the category is not modifiable by the current user.</response>
    /// <response code="404">If the category is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteCategoryAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteCategoryAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.categoryService.DeleteCategoryAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}
