using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Transactions;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for transaction management.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets a cursor-paginated list of transactions for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of transaction DTOs.</returns>
    Task<CursorPaginatedList<TransactionDto, int>> GetTransactionsAsync(TransactionQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single transaction by ID.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<TransactionDto>> GetTransactionByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new transaction for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created transaction DTO.</returns>
    Task<Result<TransactionDto>> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing transaction.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated transaction DTO.</returns>
    Task<Result<TransactionDto>> UpdateTransactionAsync(int id, UpdateTransactionRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing transaction via a JSON Patch document.
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated transaction DTO.</returns>
    Task<Result<TransactionDto>> PatchTransactionAsync(int id, JsonPatchDocument<UpdateTransactionRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a transaction (and any split children).
    /// </summary>
    /// <param name="id">The transaction ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteTransactionAsync(int id, CancellationToken cancellationToken);
}