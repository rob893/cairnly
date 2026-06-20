using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Transactions;
using Cairnly.API.Services.Auth;
using Cairnly.API.Extensions;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for transaction management.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly ILogger<TransactionService> logger;

    private readonly ITransactionRepository transactionRepository;

    private readonly IAccountRepository accountRepository;

    private readonly ICategoryRepository categoryRepository;

    private readonly ITagRepository tagRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public TransactionService(
        ILogger<TransactionService> logger,
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<TransactionDto, int>> GetTransactionsAsync(TransactionQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.transactionRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList
            .Select(TransactionDto.FromEntity)
            .ToList();

        return new CursorPaginatedList<TransactionDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<TransactionDto>> GetTransactionByIdAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await this.transactionRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (transaction == null)
        {
            this.logger.LogWarning("Transaction {TransactionId} not found", id);
            return Result<TransactionDto>.Failure(DomainErrorType.NotFound, "Transaction not found");
        }

        if (!this.CanAccess(transaction))
        {
            this.logger.LogWarning("User {UserId} attempted to access transaction {TransactionId} owned by {OwnerId}", this.currentUserService.UserId, id, transaction.UserId);
            return Result<TransactionDto>.Failure(DomainErrorType.Forbidden, "You can only access your own transactions");
        }

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(transaction));
    }

    /// <inheritdoc />
    public async Task<Result<TransactionDto>> CreateTransactionAsync(CreateTransactionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = this.currentUserService.UserId;

        var accountResult = await this.ValidateAccountAsync(request.AccountId, cancellationToken);
        if (!accountResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(accountResult.ErrorType!.Value, accountResult.ErrorMessage!);
        }

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var parentResult = await this.ValidateParentAsync(request.ParentTransactionId, request.AccountId, cancellationToken);
        if (!parentResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(parentResult.ErrorType!.Value, parentResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, userId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var transaction = new Transaction
        {
            UserId = userId,
            AccountId = request.AccountId,
            Date = request.Date,
            Amount = request.Amount,
            Merchant = request.Merchant,
            Description = request.Description,
            CategoryId = request.CategoryId,
            Source = request.Source,
            ParentTransactionId = request.ParentTransactionId,
            IsSplit = false,
            Metadata = request.Metadata ?? [],
            TransactionTags = tagIds.Select(tagId => new TransactionTag { TagId = tagId }).ToList()
        };

        this.transactionRepository.Add(transaction);

        if (parentResult.Value is { } parent)
        {
            parent.IsSplit = true;
        }

        await this.transactionRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created transaction {TransactionId} for user {UserId}", transaction.Id, transaction.UserId);

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(transaction));
    }

    /// <inheritdoc />
    public async Task<Result<TransactionDto>> UpdateTransactionAsync(int id, UpdateTransactionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var transaction = await this.transactionRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (transaction == null)
        {
            this.logger.LogWarning("Transaction {TransactionId} not found for update", id);
            return Result<TransactionDto>.Failure(DomainErrorType.NotFound, "Transaction not found");
        }

        if (!this.CanAccess(transaction))
        {
            this.logger.LogWarning("User {UserId} attempted to update transaction {TransactionId} owned by {OwnerId}", this.currentUserService.UserId, id, transaction.UserId);
            return Result<TransactionDto>.Failure(DomainErrorType.Forbidden, "You can only update your own transactions");
        }

        if (transaction.ParentTransactionId != null && request.AccountId != transaction.AccountId)
        {
            return Result<TransactionDto>.Failure(DomainErrorType.Validation, "A split child cannot be moved to a different account");
        }

        var accountResult = await this.ValidateAccountAsync(request.AccountId, cancellationToken);
        if (!accountResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(accountResult.ErrorType!.Value, accountResult.ErrorMessage!);
        }

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, transaction.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<TransactionDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        transaction.AccountId = request.AccountId;
        transaction.Date = request.Date;
        transaction.Amount = request.Amount;
        transaction.Merchant = request.Merchant;
        transaction.Description = request.Description;
        transaction.CategoryId = request.CategoryId;
        transaction.Source = request.Source;
        transaction.Metadata = request.Metadata ?? [];

        SyncTags(transaction, tagIds);

        await this.transactionRepository.SaveChangesAsync(cancellationToken);

        return Result<TransactionDto>.Success(TransactionDto.FromEntity(transaction));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteTransactionAsync(int id, CancellationToken cancellationToken)
    {
        var transaction = await this.transactionRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (transaction == null)
        {
            this.logger.LogWarning("Transaction {TransactionId} not found for deletion", id);
            return Result<bool>.Failure(DomainErrorType.NotFound, "Transaction not found");
        }

        if (!this.CanAccess(transaction))
        {
            this.logger.LogWarning("User {UserId} attempted to delete transaction {TransactionId} owned by {OwnerId}", this.currentUserService.UserId, id, transaction.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own transactions");
        }

        this.transactionRepository.Remove(transaction);
        await this.transactionRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted transaction {TransactionId} for user {UserId}", id, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<TransactionDto>> PatchTransactionAsync(int id, JsonPatchDocument<UpdateTransactionRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<TransactionDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var transaction = await this.transactionRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (transaction == null)
        {
            return Result<TransactionDto>.Failure(DomainErrorType.NotFound, "Transaction not found");
        }

        if (!this.CanAccess(transaction))
        {
            this.logger.LogWarning("User {UserId} attempted to patch transaction {TransactionId} owned by {OwnerId}", this.currentUserService.UserId, id, transaction.UserId);
            return Result<TransactionDto>.Failure(DomainErrorType.Forbidden, "You can only update your own transactions");
        }

        var request = UpdateTransactionRequest.FromEntity(transaction);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<TransactionDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<TransactionDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateTransactionAsync(id, request, cancellationToken);
    }

    private static List<int> NormalizeTagIds(IReadOnlyList<int>? tagIds)
    {
        return tagIds == null ? [] : tagIds.Distinct().ToList();
    }

    private static void SyncTags(Transaction transaction, List<int> desiredTagIds)
    {
        var desired = desiredTagIds.ToHashSet();
        var existing = transaction.TransactionTags.Select(tt => tt.TagId).ToHashSet();

        transaction.TransactionTags.RemoveAll(tt => !desired.Contains(tt.TagId));

        foreach (var tagId in desired.Where(tagId => !existing.Contains(tagId)))
        {
            transaction.TransactionTags.Add(new TransactionTag { TransactionId = transaction.Id, TagId = tagId });
        }
    }

    private bool CanAccess(Transaction transaction)
    {
        return transaction.UserId == this.currentUserService.UserId || this.currentUserService.IsAdmin;
    }

    private async Task<Result<bool>> ValidateAccountAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await this.accountRepository.GetByIdAsync(accountId, track: false, cancellationToken);

        if (account == null || (account.UserId != this.currentUserService.UserId && !this.currentUserService.IsAdmin))
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "The specified account does not exist");
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<bool>> ValidateCategoryAsync(int? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == null)
        {
            return Result<bool>.Success(true);
        }

        var category = await this.categoryRepository.GetByIdAsync(categoryId.Value, track: false, cancellationToken);

        var accessible = category != null
            && (category.UserId == this.currentUserService.UserId || category.IsSystem || this.currentUserService.IsAdmin);

        if (!accessible)
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "The specified category does not exist");
        }

        return Result<bool>.Success(true);
    }

    private async Task<Result<Transaction?>> ValidateParentAsync(int? parentId, int accountId, CancellationToken cancellationToken)
    {
        if (parentId == null)
        {
            return Result<Transaction?>.Success(null);
        }

        var parent = await this.transactionRepository.GetByIdAsync(parentId.Value, track: true, cancellationToken);

        if (parent == null || (parent.UserId != this.currentUserService.UserId && !this.currentUserService.IsAdmin))
        {
            return Result<Transaction?>.Failure(DomainErrorType.Validation, "The specified parent transaction does not exist");
        }

        if (parent.ParentTransactionId != null)
        {
            return Result<Transaction?>.Failure(DomainErrorType.Validation, "Split transactions cannot be nested");
        }

        if (parent.AccountId != accountId)
        {
            return Result<Transaction?>.Failure(DomainErrorType.Validation, "A split child must be on the same account as its parent");
        }

        return Result<Transaction?>.Success(parent);
    }

    private async Task<Result<bool>> ValidateTagsAsync(List<int> tagIds, int ownerUserId, CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return Result<bool>.Success(true);
        }

        var tags = await this.tagRepository.SearchAsync(
            t => tagIds.Contains(t.Id) && t.UserId == ownerUserId,
            track: false,
            cancellationToken);

        if (tags.Count != tagIds.Count)
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "One or more of the specified tags do not exist");
        }

        return Result<bool>.Success(true);
    }
}
