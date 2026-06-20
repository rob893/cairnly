using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.BudgetExpenses;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing a budget's expense line items.
/// </summary>
[Route("api/v{version:apiVersion}/budgets/{budgetId}/expenses")]
[ApiVersion("1")]
[ApiController]
public sealed class BudgetExpensesController : ServiceControllerBase
{
    private readonly IBudgetExpenseService expenseService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetExpensesController"/> class.
    /// </summary>
    /// <param name="expenseService">The budget expense service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public BudgetExpensesController(IBudgetExpenseService expenseService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of expense line items for a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of expense DTOs.</returns>
    /// <response code="200">Returns the paginated list of expense line items.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpGet(Name = nameof(GetExpensesAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CursorPaginatedResponse<BudgetExpenseDto>>> GetExpensesAsync([FromRoute] int budgetId, [FromQuery] BudgetExpenseQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.GetExpensesAsync(budgetId, queryParameters, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow.ToCursorPaginatedResponse(queryParameters));
    }

    /// <summary>
    /// Gets a specific expense line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The expense DTO.</returns>
    /// <response code="200">Returns the expense line item.</response>
    /// <response code="403">If the expense does not belong to the current user.</response>
    /// <response code="404">If the budget or expense is not found.</response>
    [HttpGet("{expenseId}", Name = nameof(GetExpenseByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetExpenseDto>> GetExpenseByIdAsync([FromRoute] int budgetId, [FromRoute] int expenseId, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.GetExpenseByIdAsync(budgetId, expenseId, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new expense line item in a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="request">The create expense request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created expense line item.</returns>
    /// <response code="201">The expense line was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the budget does not belong to the current user.</response>
    /// <response code="404">If the budget is not found.</response>
    [HttpPost(Name = nameof(CreateExpenseAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetExpenseDto>> CreateExpenseAsync([FromRoute] int budgetId, [FromBody] CreateBudgetExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.CreateExpenseAsync(budgetId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var expense = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetExpenseByIdAsync), new { budgetId, expenseId = expense.Id }, expense);
    }

    /// <summary>
    /// Updates an existing expense line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="request">The update expense request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated expense line item.</returns>
    /// <response code="200">Returns the updated expense line item.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the expense does not belong to the current user.</response>
    /// <response code="404">If the budget or expense is not found.</response>
    [HttpPut("{expenseId}", Name = nameof(UpdateExpenseAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetExpenseDto>> UpdateExpenseAsync([FromRoute] int budgetId, [FromRoute] int expenseId, [FromBody] UpdateBudgetExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.UpdateExpenseAsync(budgetId, expenseId, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing expense line item using a JSON Patch document.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated expense line item.</returns>
    /// <response code="200">Returns the updated expense line item.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the expense does not belong to the current user.</response>
    /// <response code="404">If the budget or expense is not found.</response>
    [HttpPatch("{expenseId}", Name = nameof(PatchExpenseAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetExpenseDto>> PatchExpenseAsync([FromRoute] int budgetId, [FromRoute] int expenseId, [FromBody] JsonPatchDocument<UpdateBudgetExpenseRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.PatchExpenseAsync(budgetId, expenseId, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes an expense line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The expense line was deleted successfully.</response>
    /// <response code="403">If the expense does not belong to the current user.</response>
    /// <response code="404">If the budget or expense is not found.</response>
    [HttpDelete("{expenseId}", Name = nameof(DeleteExpenseAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteExpenseAsync([FromRoute] int budgetId, [FromRoute] int expenseId, CancellationToken cancellationToken)
    {
        var result = await this.expenseService.DeleteExpenseAsync(budgetId, expenseId, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}
