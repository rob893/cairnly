using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.BudgetIncomes;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing a budget's income line items.
/// </summary>
[Route("api/v{version:apiVersion}/budgets/{budgetId}/income")]
[ApiVersion("1")]
[ApiController]
public sealed class BudgetIncomesController : ServiceControllerBase
{
    private readonly IBudgetIncomeService incomeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetIncomesController"/> class.
    /// </summary>
    /// <param name="incomeService">The budget income service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public BudgetIncomesController(IBudgetIncomeService incomeService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of income line items for a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of income DTOs.</returns>
    /// <response code="200">Returns the paginated list of income line items.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpGet(Name = nameof(GetIncomesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CursorPaginatedResponse<BudgetIncomeDto>>> GetIncomesAsync([FromRoute] int budgetId, [FromQuery] BudgetIncomeQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.GetIncomesAsync(budgetId, queryParameters, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow.ToCursorPaginatedResponse(queryParameters));
    }

    /// <summary>
    /// Gets a specific income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The income DTO.</returns>
    /// <response code="200">Returns the income line item.</response>
    /// <response code="403">If the income does not belong to the current user.</response>
    /// <response code="404">If the budget or income is not found.</response>
    [HttpGet("{incomeId}", Name = nameof(GetIncomeByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetIncomeDto>> GetIncomeByIdAsync([FromRoute] int budgetId, [FromRoute] int incomeId, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.GetIncomeByIdAsync(budgetId, incomeId, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new income line item in a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="request">The create income request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created income line item.</returns>
    /// <response code="201">The income line was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpPost(Name = nameof(CreateIncomeAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetIncomeDto>> CreateIncomeAsync([FromRoute] int budgetId, [FromBody] CreateBudgetIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.CreateIncomeAsync(budgetId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var income = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetIncomeByIdAsync), new { budgetId, incomeId = income.Id }, income);
    }

    /// <summary>
    /// Updates an existing income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="request">The update income request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated income line item.</returns>
    /// <response code="200">Returns the updated income line item.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the income does not belong to the current user.</response>
    /// <response code="404">If the budget or income is not found.</response>
    [HttpPut("{incomeId}", Name = nameof(UpdateIncomeAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetIncomeDto>> UpdateIncomeAsync([FromRoute] int budgetId, [FromRoute] int incomeId, [FromBody] UpdateBudgetIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.UpdateIncomeAsync(budgetId, incomeId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing income line item using a JSON Patch document.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated income line item.</returns>
    /// <response code="200">Returns the updated income line item.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the income does not belong to the current user.</response>
    /// <response code="404">If the budget or income is not found.</response>
    [HttpPatch("{incomeId}", Name = nameof(PatchIncomeAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetIncomeDto>> PatchIncomeAsync([FromRoute] int budgetId, [FromRoute] int incomeId, [FromBody] JsonPatchDocument<UpdateBudgetIncomeRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.PatchIncomeAsync(budgetId, incomeId, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes an income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The income line was deleted successfully.</response>
    /// <response code="403">If the income does not belong to the current user.</response>
    /// <response code="404">If the budget or income is not found.</response>
    [HttpDelete("{incomeId}", Name = nameof(DeleteIncomeAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteIncomeAsync([FromRoute] int budgetId, [FromRoute] int incomeId, CancellationToken cancellationToken)
    {
        var result = await this.incomeService.DeleteIncomeAsync(budgetId, incomeId, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}
