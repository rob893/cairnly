using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Budgets;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing budgets.
/// </summary>
[Route("api/v{version:apiVersion}/budgets")]
[ApiVersion("1")]
[ApiController]
public sealed class BudgetsController : ServiceControllerBase
{
    private readonly IBudgetService budgetService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetsController"/> class.
    /// </summary>
    /// <param name="budgetService">The budget service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public BudgetsController(IBudgetService budgetService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.budgetService = budgetService ?? throw new ArgumentNullException(nameof(budgetService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of budgets for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of budget DTOs.</returns>
    /// <response code="200">Returns the paginated list of budgets.</response>
    [HttpGet(Name = nameof(GetBudgetsAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<BudgetDto>>> GetBudgetsAsync([FromQuery] BudgetQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var budgets = await this.budgetService.GetBudgetsAsync(queryParameters, cancellationToken);
        var response = budgets.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific budget by ID.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The budget DTO.</returns>
    /// <response code="200">Returns the budget.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpGet("{id}", Name = nameof(GetBudgetByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> GetBudgetByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.GetBudgetByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Gets the computed summary (normalized income, expenses, and remaining) for a budget.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The budget summary.</returns>
    /// <response code="200">Returns the budget summary.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpGet("{id}/summary", Name = nameof(GetBudgetSummaryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetSummaryDto>> GetBudgetSummaryAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.GetBudgetSummaryAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new budget for the current user.
    /// </summary>
    /// <param name="request">The create budget request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created budget.</returns>
    /// <response code="201">The budget was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost(Name = nameof(CreateBudgetAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetDto>> CreateBudgetAsync([FromBody] CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.CreateBudgetAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var budget = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetBudgetByIdAsync), new { id = budget.Id }, budget);
    }

    /// <summary>
    /// Updates an existing budget.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="request">The update budget request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated budget.</returns>
    /// <response code="200">Returns the updated budget.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpPut("{id}", Name = nameof(UpdateBudgetAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> UpdateBudgetAsync([FromRoute] int id, [FromBody] UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.UpdateBudgetAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing budget using a JSON Patch document.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated budget.</returns>
    /// <response code="200">Returns the updated budget.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpPatch("{id}", Name = nameof(PatchBudgetAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetDto>> PatchBudgetAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateBudgetRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.PatchBudgetAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes a budget and its line items.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The budget was deleted successfully.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteBudgetAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteBudgetAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.budgetService.DeleteBudgetAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}
