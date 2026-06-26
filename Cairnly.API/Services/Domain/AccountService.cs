using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Accounts;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for account management.
/// </summary>
public sealed class AccountService : IAccountService
{
    private readonly ILogger<AccountService> logger;

    private readonly IAccountRepository accountRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public AccountService(
        ILogger<AccountService> logger,
        IAccountRepository accountRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<AccountDto, int>> GetAccountsAsync(AccountQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.accountRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var balances = await this.ResolveBalancesAsync(pagedList.ToList(), cancellationToken);

        var mapped = pagedList
            .Select(account => AccountDto.FromEntity(account, balances[account.Id]))
            .ToList();

        return new CursorPaginatedList<AccountDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<AccountDto>> GetAccountByIdAsync(int id, CancellationToken cancellationToken)
    {
        var account = await this.accountRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (account == null)
        {
            this.logger.LogWarning("Account {AccountId} not found", id);
            return Result<AccountDto>.Failure(DomainErrorType.NotFound, "Account not found");
        }

        if (!this.CanAccess(account))
        {
            this.logger.LogWarning("User {UserId} attempted to access account {AccountId} owned by {OwnerId}", this.currentUserService.UserId, id, account.UserId);
            return Result<AccountDto>.Failure(DomainErrorType.Forbidden, "You can only access your own accounts");
        }

        var balances = await this.ResolveBalancesAsync([account], cancellationToken);

        return Result<AccountDto>.Success(AccountDto.FromEntity(account, balances[account.Id]));
    }

    /// <inheritdoc />
    public async Task<Result<AccountDto>> CreateAccountAsync(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = new Account
        {
            UserId = this.currentUserService.UserId,
            Name = request.Name,
            Type = request.Type,
            Class = request.Class,
            Currency = request.Currency.ToUpperInvariant(),
            OpeningBalance = request.OpeningBalance,
            CurrentBalance = request.IsManual ? request.CurrentBalance : 0,
            IsManual = request.IsManual,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.accountRepository.Add(account);
        await this.accountRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created account {AccountId} for user {UserId}", account.Id, account.UserId);

        var balance = account.IsManual ? account.CurrentBalance : account.OpeningBalance;

        return Result<AccountDto>.Success(AccountDto.FromEntity(account, balance));
    }

    /// <inheritdoc />
    public async Task<Result<AccountDto>> UpdateAccountAsync(int id, UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await this.accountRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (account == null)
        {
            this.logger.LogWarning("Account {AccountId} not found for update", id);
            return Result<AccountDto>.Failure(DomainErrorType.NotFound, "Account not found");
        }

        if (!this.CanAccess(account))
        {
            this.logger.LogWarning("User {UserId} attempted to update account {AccountId} owned by {OwnerId}", this.currentUserService.UserId, id, account.UserId);
            return Result<AccountDto>.Failure(DomainErrorType.Forbidden, "You can only update your own accounts");
        }

        account.Name = request.Name;
        account.Type = request.Type;
        account.Class = request.Class;
        account.Currency = request.Currency.ToUpperInvariant();
        account.OpeningBalance = request.OpeningBalance;
        account.IsManual = request.IsManual;
        account.CurrentBalance = request.IsManual ? request.CurrentBalance : 0;
        account.Metadata = request.Metadata ?? [];
        account.UpdatedById = this.currentUserService.UserId;

        await this.accountRepository.SaveChangesAsync(cancellationToken);

        var balances = await this.ResolveBalancesAsync([account], cancellationToken);

        return Result<AccountDto>.Success(AccountDto.FromEntity(account, balances[account.Id]));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAccountAsync(int id, CancellationToken cancellationToken)
    {
        var account = await this.accountRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (account == null)
        {
            this.logger.LogWarning("Account {AccountId} not found for deletion", id);
            return Result<bool>.Failure(DomainErrorType.NotFound, "Account not found");
        }

        if (!this.CanAccess(account))
        {
            this.logger.LogWarning("User {UserId} attempted to delete account {AccountId} owned by {OwnerId}", this.currentUserService.UserId, id, account.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own accounts");
        }

        this.accountRepository.Remove(account);
        await this.accountRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted account {AccountId} for user {UserId}", id, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<AccountDto>> PatchAccountAsync(int id, JsonPatchDocument<UpdateAccountRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<AccountDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var account = await this.accountRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (account == null)
        {
            return Result<AccountDto>.Failure(DomainErrorType.NotFound, "Account not found");
        }

        if (!this.CanAccess(account))
        {
            this.logger.LogWarning("User {UserId} attempted to patch account {AccountId} owned by {OwnerId}", this.currentUserService.UserId, id, account.UserId);
            return Result<AccountDto>.Failure(DomainErrorType.Forbidden, "You can only update your own accounts");
        }

        var request = UpdateAccountRequest.FromEntity(account);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<AccountDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<AccountDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateAccountAsync(id, request, cancellationToken);
    }

    private bool CanAccess(Account account)
    {
        return account.UserId == this.currentUserService.UserId || this.currentUserService.IsAdmin;
    }

    /// <summary>
    /// Resolves the current balance for each account: stored value for manual accounts, opening
    /// balance plus the sum of transactions for derived accounts (computed in a single query).
    /// </summary>
    private async Task<IReadOnlyDictionary<int, long>> ResolveBalancesAsync(List<Account> accounts, CancellationToken cancellationToken)
    {
        var derivedIds = accounts.Where(a => !a.IsManual).Select(a => a.Id).ToList();

        var sums = derivedIds.Count > 0
            ? await this.accountRepository.GetTransactionSumsAsync(derivedIds, cancellationToken)
            : new Dictionary<int, long>();

        var balances = new Dictionary<int, long>(accounts.Count);

        foreach (var account in accounts)
        {
            balances[account.Id] = account.IsManual
                ? account.CurrentBalance
                : account.OpeningBalance + (sums.TryGetValue(account.Id, out var sum) ? sum : 0);
        }

        return balances;
    }
}