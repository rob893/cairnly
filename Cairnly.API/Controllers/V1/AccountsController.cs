using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Accounts;
using Cairnly.API.Models.Responses.Pagination;
using Cairnly.API.Services.Core;
using Cairnly.API.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Cairnly.API.Controllers.V1;

/// <summary>
/// Controller for managing financial accounts.
/// </summary>
[Route("api/v{version:apiVersion}/accounts")]
[ApiVersion("1")]
[ApiController]
public sealed class AccountsController : ServiceControllerBase
{
    private readonly IAccountService accountService;

    private readonly IBalanceHistoryService balanceHistoryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountsController"/> class.
    /// </summary>
    /// <param name="accountService">The account service.</param>
    /// <param name="balanceHistoryService">The balance history service.</param>
    /// <param name="correlationIdService">The correlation ID service.</param>
    public AccountsController(IAccountService accountService, IBalanceHistoryService balanceHistoryService, ICorrelationIdService correlationIdService)
        : base(correlationIdService)
    {
        this.accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        this.balanceHistoryService = balanceHistoryService ?? throw new ArgumentNullException(nameof(balanceHistoryService));
    }

    /// <summary>
    /// Gets the current user's net-worth history for the selected window.
    /// </summary>
    /// <param name="queryParameters">The history query parameters (timeframe).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The net-worth history.</returns>
    /// <response code="200">Returns the net-worth history.</response>
    [HttpGet("net-worth", Name = nameof(GetNetWorthHistoryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<NetWorthHistoryDto>> GetNetWorthHistoryAsync([FromQuery] BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var history = await this.balanceHistoryService.GetNetWorthHistoryAsync(queryParameters, cancellationToken);

        return this.Ok(history);
    }

    /// <summary>
    /// Gets per-account balance history for the current user over the selected window.
    /// </summary>
    /// <param name="queryParameters">The history query parameters (timeframe).</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The per-account balance history series.</returns>
    /// <response code="200">Returns the per-account balance history.</response>
    [HttpGet("history", Name = nameof(GetAccountHistoryAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AccountHistoryDto>>> GetAccountHistoryAsync([FromQuery] BalanceHistoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var history = await this.balanceHistoryService.GetAccountHistoryAsync(queryParameters, cancellationToken);

        return this.Ok(history);
    }

    /// <summary>
    /// Gets a cursor-paginated list of accounts for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of account DTOs.</returns>
    /// <response code="200">Returns the paginated list of accounts.</response>
    [HttpGet(Name = nameof(GetAccountsAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CursorPaginatedResponse<AccountDto>>> GetAccountsAsync([FromQuery] AccountQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        var accounts = await this.accountService.GetAccountsAsync(queryParameters, cancellationToken);
        var response = accounts.ToCursorPaginatedResponse(queryParameters);

        return this.Ok(response);
    }

    /// <summary>
    /// Gets a specific account by ID.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The account DTO.</returns>
    /// <response code="200">Returns the account.</response>
    /// <response code="403">If the account does not belong to the current user.</response>
    /// <response code="404">If the account is not found.</response>
    [HttpGet("{id}", Name = nameof(GetAccountByIdAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> GetAccountByIdAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.accountService.GetAccountByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Creates a new account for the current user.
    /// </summary>
    /// <param name="request">The create account request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created account.</returns>
    /// <response code="201">The account was created successfully.</response>
    /// <response code="400">If the request is invalid.</response>
    [HttpPost(Name = nameof(CreateAccountAsync))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AccountDto>> CreateAccountAsync([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await this.accountService.CreateAccountAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        var account = result.ValueOrThrow;

        return this.CreatedAtRoute(nameof(this.GetAccountByIdAsync), new { id = account.Id }, account);
    }

    /// <summary>
    /// Updates an existing account.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="request">The update account request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated account.</returns>
    /// <response code="200">Returns the updated account.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the account does not belong to the current user.</response>
    /// <response code="404">If the account is not found.</response>
    [HttpPut("{id}", Name = nameof(UpdateAccountAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> UpdateAccountAsync([FromRoute] int id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await this.accountService.UpdateAccountAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Partially updates an existing account using a JSON Patch document.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated account.</returns>
    /// <response code="200">Returns the updated account.</response>
    /// <response code="400">If the request or patch document is invalid.</response>
    /// <response code="403">If the account does not belong to the current user.</response>
    /// <response code="404">If the account is not found.</response>
    [HttpPatch("{id}", Name = nameof(PatchAccountAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> PatchAccountAsync([FromRoute] int id, [FromBody] JsonPatchDocument<UpdateAccountRequest> patchDocument, CancellationToken cancellationToken)
    {
        var result = await this.accountService.PatchAccountAsync(id, patchDocument, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }

    /// <summary>
    /// Deletes an account and its transactions.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">The account was deleted successfully.</response>
    /// <response code="403">If the account does not belong to the current user.</response>
    /// <response code="404">If the account is not found.</response>
    [HttpDelete("{id}", Name = nameof(DeleteAccountAsync))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAccountAsync([FromRoute] int id, CancellationToken cancellationToken)
    {
        var result = await this.accountService.DeleteAccountAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.NoContent();
    }

    /// <summary>
    /// Sets an account's balance to a target value as of a given day by recording a
    /// balance-adjustment transaction for the difference.
    /// </summary>
    /// <param name="id">The account ID.</param>
    /// <param name="request">The set-balance request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated account.</returns>
    /// <response code="200">Returns the updated account.</response>
    /// <response code="400">If the request is invalid.</response>
    /// <response code="403">If the account does not belong to the current user.</response>
    /// <response code="404">If the account is not found.</response>
    [HttpPut("{id}/balance", Name = nameof(SetAccountBalanceAsync))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountDto>> SetAccountBalanceAsync([FromRoute] int id, [FromBody] SetAccountBalanceRequest request, CancellationToken cancellationToken)
    {
        var result = await this.accountService.SetBalanceAsync(id, request, cancellationToken);

        if (!result.IsSuccess)
        {
            return this.HandleServiceFailureResult(result);
        }

        return this.Ok(result.ValueOrThrow);
    }
}