using System;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Transactions;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing transactions.
/// </summary>
[Route("api/v{version:apiVersion}/transactions")]
[ApiVersion("1")]
[ApiController]
public sealed class TransactionsController : ServiceControllerBase
{
    private readonly ITransactionService transactionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="transactionService">The transaction service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public TransactionsController(ITransactionService transactionService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
    }

    /// <summary>
    /// Gets a cursor-paginated list of transactions for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of transaction DTOs.</returns>
    /// <response code="200">Returns the paginated list of transactions.</response>
    [HttpGet(Name = nameof(GetTransactionsAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<TransactionDto>>> GetTransactionsAsync([FromQuery] TransactionQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var transactions = await this.transactionService.GetTransactionsAsync(queryParameters, cancellationToken);
        var response = transactions.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction DTO.</returns>
    /// <response code="200">Returns the transaction.</response>
    /// <response code="403">If the transaction does not belong to the current user.</response>
    /// <response code="404">If the transaction is not found.</response>
    [HttpGet("{id}", Name = nameof(GetTransactionByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> GetTransactionByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.transactionService.GetTransactionByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new transaction for the current user.
    /// </summary>
    /// <param name="request">The create transaction request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created transaction.</returns>
    /// <response code="201">The transaction was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost(Name = nameof(CreateTransactionAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionDto>> CreateTransactionAsync([FromBody] CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.transactionService.CreateTransactionAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var transaction = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetTransactionByIdAsync), new { id = transaction.Id }, transaction);
    }

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="request">The update transaction request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated transaction.</returns>
    /// <response code="200">Returns the updated transaction.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the transaction does not belong to the current user.</response>
    /// <response code="404">If the transaction is not found.</response>
    [HttpPut("{id}", Name = nameof(UpdateTransactionAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> UpdateTransactionAsync([FromRoute] int id, [FromBody] UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        var result = await this.transactionService.UpdateTransactionAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing transaction using a JSON Patch document.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated transaction.</returns>
    /// <response code="200">Returns the updated transaction.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the transaction does not belong to the current user.</response>
    /// <response code="404">If the transaction is not found.</response>
    [HttpPatch("{id}", Name = nameof(PatchTransactionAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDto>> PatchTransactionAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateTransactionRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.transactionService.PatchTransactionAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes a transaction and any of its split children.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The transaction was deleted successfully.</response>
    /// <response code="403">If the transaction does not belong to the current user.</response>
    /// <response code="404">If the transaction is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteTransactionAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTransactionAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.transactionService.DeleteTransactionAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }
}