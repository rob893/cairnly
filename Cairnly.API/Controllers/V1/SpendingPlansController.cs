using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlans;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing spendingPlans.
/// </summary>
[Route("api/v{version:apiVersion}/spending-plans")]
[ApiVersion("1")]
[ApiController]
public sealed class SpendingPlansController : ServiceControllerBase
{
    private readonly ISpendingPlanService spendingPlanService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlansController"/> class.
    /// </summary>
    /// <param name="spendingPlanService">The spendingPlan service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public SpendingPlansController(ISpendingPlanService spendingPlanService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.spendingPlanService = spendingPlanService ?? throw new ArgumentNullException(nameof(spendingPlanService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of spendingPlans for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of spendingPlan DTOs.</returns>
    /// <response code="200">Returns the paginated list of spendingPlans.</response>
    [HttpGet(Name = nameof(GetSpendingPlansAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<SpendingPlanDto>>> GetSpendingPlansAsync([FromQuery] SpendingPlanQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var spendingPlans = await this.spendingPlanService.GetSpendingPlansAsync(queryParameters, cancellationToken);
        var response = spendingPlans.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific spendingPlan by ID.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The spendingPlan DTO.</returns>
    /// <response code="200">Returns the spendingPlan.</response>
    /// <response code="403">If the spendingPlan does not belong to the current user.</response>
    /// <response code="404">If the spendingPlan is not found.</response>
    [HttpGet("{id}", Name = nameof(GetSpendingPlanByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendingPlanDto>> GetSpendingPlanByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.GetSpendingPlanByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Gets computed summaries for all spendingPlans accessible to the current user.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current user's spendingPlan summaries.</returns>
    /// <response code="200">Returns the spendingPlan summaries.</response>
    [HttpGet("summaries", Name = nameof(GetSpendingPlanSummariesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpendingPlanSummaryDto>>> GetSpendingPlanSummariesAsync(CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.GetSpendingPlanSummariesAsync(cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Gets the computed summary (normalized income, expenses, and remaining) for a spendingPlan.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The spendingPlan summary.</returns>
    /// <response code="200">Returns the spendingPlan summary.</response>
    /// <response code="403">If the spendingPlan does not belong to the current user.</response>
    /// <response code="404">If the spendingPlan is not found.</response>
    [HttpGet("{id}/summary", Name = nameof(GetSpendingPlanSummaryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendingPlanSummaryDto>> GetSpendingPlanSummaryAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.GetSpendingPlanSummaryAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new spendingPlan for the current user.
    /// </summary>
    /// <param name="request">The create spendingPlan request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created spendingPlan.</returns>
    /// <response code="201">The spendingPlan was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost(Name = nameof(CreateSpendingPlanAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpendingPlanDto>> CreateSpendingPlanAsync([FromBody] CreateSpendingPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.CreateSpendingPlanAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var spendingPlan = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetSpendingPlanByIdAsync), new { id = spendingPlan.Id }, spendingPlan);
    }

    /// <summary>
    /// Updates an existing spendingPlan.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="request">The update spendingPlan request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated spendingPlan.</returns>
    /// <response code="200">Returns the updated spendingPlan.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the spendingPlan does not belong to the current user.</response>
    /// <response code="404">If the spendingPlan is not found.</response>
    [HttpPut("{id}", Name = nameof(UpdateSpendingPlanAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendingPlanDto>> UpdateSpendingPlanAsync([FromRoute] int id, [FromBody] UpdateSpendingPlanRequest request, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.UpdateSpendingPlanAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing spendingPlan using a JSON Patch document.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated spendingPlan.</returns>
    /// <response code="200">Returns the updated spendingPlan.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the spendingPlan does not belong to the current user.</response>
    /// <response code="404">If the spendingPlan is not found.</response>
    [HttpPatch("{id}", Name = nameof(PatchSpendingPlanAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpendingPlanDto>> PatchSpendingPlanAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateSpendingPlanRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.PatchSpendingPlanAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes a spendingPlan and its line items.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The spendingPlan was deleted successfully.</response>
    /// <response code="403">If the spendingPlan does not belong to the current user.</response>
    /// <response code="404">If the spendingPlan is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteSpendingPlanAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSpendingPlanAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.spendingPlanService.DeleteSpendingPlanAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}