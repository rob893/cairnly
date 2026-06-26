using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlanExpenses;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for managing a spendingPlan's expense line items.
/// </summary>
public sealed class SpendingPlanExpenseService : ISpendingPlanExpenseService
{
    private readonly ILogger<SpendingPlanExpenseService> logger;

    private readonly ISpendingPlanExpenseRepository expenseRepository;

    private readonly ISpendingPlanRepository spendingPlanRepository;

    private readonly ICategoryTagValidator categoryTagValidator;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanExpenseService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="expenseRepository">The spendingPlan expense repository.</param>
    /// <param name="spendingPlanRepository">The spendingPlan repository.</param>
    /// <param name="categoryTagValidator">The category/tag validator.</param>
    /// <param name="currentUserService">The current user service.</param>
    public SpendingPlanExpenseService(
        ILogger<SpendingPlanExpenseService> logger,
        ISpendingPlanExpenseRepository expenseRepository,
        ISpendingPlanRepository spendingPlanRepository,
        ICategoryTagValidator categoryTagValidator,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        this.spendingPlanRepository = spendingPlanRepository ?? throw new ArgumentNullException(nameof(spendingPlanRepository));
        this.categoryTagValidator = categoryTagValidator ?? throw new ArgumentNullException(nameof(categoryTagValidator));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<CursorPaginatedList<SpendingPlanExpenseDto, int>>> GetExpensesAsync(int spendingPlanId, SpendingPlanExpenseQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<CursorPaginatedList<SpendingPlanExpenseDto, int>>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        queryParameters.SpendingPlanId = spendingPlanId;
        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.expenseRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(SpendingPlanExpenseDto.FromEntity).ToList();

        var result = new CursorPaginatedList<SpendingPlanExpenseDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);

        return Result<CursorPaginatedList<SpendingPlanExpenseDto, int>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanExpenseDto>> GetExpenseByIdAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken)
    {
        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(expense, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanExpenseDto>.Failure(notFound.Value, error);
        }

        return Result<SpendingPlanExpenseDto>.Success(SpendingPlanExpenseDto.FromEntity(expense!));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanExpenseDto>> CreateExpenseAsync(int spendingPlanId, CreateSpendingPlanExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<SpendingPlanExpenseDto>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        var spendingPlan = spendingPlanResult.ValueOrThrow;

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<SpendingPlanExpenseDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, spendingPlan.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<SpendingPlanExpenseDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var expense = new SpendingPlanExpense
        {
            UserId = spendingPlan.UserId,
            SpendingPlanId = spendingPlan.Id,
            Name = request.Name,
            Description = request.Description,
            Amount = request.Amount,
            Cadence = request.Cadence,
            CategoryId = request.CategoryId,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId,
            SpendingPlanExpenseTags = tagIds.Select(tagId => new SpendingPlanExpenseTag { TagId = tagId }).ToList()
        };

        this.expenseRepository.Add(expense);
        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created spendingPlan expense {ExpenseId} in spendingPlan {SpendingPlanId} for user {UserId}", expense.Id, spendingPlan.Id, spendingPlan.UserId);

        return Result<SpendingPlanExpenseDto>.Success(SpendingPlanExpenseDto.FromEntity(expense));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanExpenseDto>> UpdateExpenseAsync(int spendingPlanId, int expenseId, UpdateSpendingPlanExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(expense, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanExpenseDto>.Failure(notFound.Value, error);
        }

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<SpendingPlanExpenseDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, expense!.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<SpendingPlanExpenseDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        expense.Name = request.Name;
        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.Cadence = request.Cadence;
        expense.CategoryId = request.CategoryId;
        expense.Metadata = request.Metadata ?? [];
        expense.UpdatedById = this.currentUserService.UserId;

        TagLinkUtilities.Sync(expense.SpendingPlanExpenseTags, tagIds, tagId => new SpendingPlanExpenseTag { SpendingPlanExpenseId = expense.Id, TagId = tagId });

        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        return Result<SpendingPlanExpenseDto>.Success(SpendingPlanExpenseDto.FromEntity(expense));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanExpenseDto>> PatchExpenseAsync(int spendingPlanId, int expenseId, JsonPatchDocument<UpdateSpendingPlanExpenseRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<SpendingPlanExpenseDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(expense, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanExpenseDto>.Failure(notFound.Value, error);
        }

        var request = UpdateSpendingPlanExpenseRequest.FromEntity(expense!);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<SpendingPlanExpenseDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<SpendingPlanExpenseDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateExpenseAsync(spendingPlanId, expenseId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteExpenseAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken)
    {
        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(expense, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<bool>.Failure(notFound.Value, error);
        }

        this.expenseRepository.Remove(expense!);
        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted spendingPlan expense {ExpenseId} from spendingPlan {SpendingPlanId} for user {UserId}", expenseId, spendingPlanId, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Validates that a line item exists, belongs to the given spendingPlan, and is accessible. Returns the
    /// failure error type (with message) when invalid, or <c>null</c> when valid.
    /// </summary>
    private DomainErrorType? ResolveLineItem(SpendingPlanExpense? expense, int spendingPlanId, out string error)
    {
        if (expense == null || expense.SpendingPlanId != spendingPlanId)
        {
            error = "Expense line not found";
            return DomainErrorType.NotFound;
        }

        if (!this.currentUserService.IsUserAuthorizedForResource(expense))
        {
            this.logger.LogWarning("User {UserId} attempted to access spendingPlan expense {ExpenseId} owned by {OwnerId}", this.currentUserService.UserId, expense.Id, expense.UserId);
            error = "You can only access your own spending plan expenses";
            return DomainErrorType.Forbidden;
        }

        error = string.Empty;
        return null;
    }

    private async Task<Result<SpendingPlan>> VerifySpendingPlanAsync(int spendingPlanId, CancellationToken cancellationToken)
    {
        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(spendingPlanId, track: false, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<SpendingPlan>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.currentUserService.IsUserAuthorizedForResource(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to access spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, spendingPlanId, spendingPlan.UserId);
            return Result<SpendingPlan>.Failure(DomainErrorType.Forbidden, "You can only access your own spending plans");
        }

        return Result<SpendingPlan>.Success(spendingPlan);
    }
}