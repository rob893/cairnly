using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Accounts;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for account management.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Gets a cursor-paginated list of accounts for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of account DTOs.</returns>
    Task<CursorPaginatedList<AccountDto, int>> GetAccountsAsync(AccountQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single account by ID.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The account DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<AccountDto>> GetAccountByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new account for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created account DTO.</returns>
    Task<Result<AccountDto>> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account DTO.</returns>
    Task<Result<AccountDto>> UpdateAccountAsync(int id, UpdateAccountRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing account via a JSON Patch document.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated account DTO.</returns>
    Task<Result<AccountDto>> PatchAccountAsync(int id, JsonPatchDocument<UpdateAccountRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an account and its transactions.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteAccountAsync(int id, CancellationToken cancellationToken);
}
