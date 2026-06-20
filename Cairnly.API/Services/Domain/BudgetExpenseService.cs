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
using Cairnly.API.Models.Requests.BudgetExpenses;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for managing a budget's expense line items.
/// </summary>
public sealed class BudgetExpenseService : IBudgetExpenseService
{
    private readonly ILogger<BudgetExpenseService> logger;

    private readonly IBudgetExpenseRepository expenseRepository;

    private readonly IBudgetRepository budgetRepository;

    private readonly ICategoryRepository categoryRepository;

    private readonly ITagRepository tagRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetExpenseService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="expenseRepository">The budget expense repository.</param>
    /// <param name="budgetRepository">The budget repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public BudgetExpenseService(
        ILogger<BudgetExpenseService> logger,
        IBudgetExpenseRepository expenseRepository,
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        this.budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<CursorPaginatedList<BudgetExpenseDto, int>>> GetExpensesAsync(int budgetId, BudgetExpenseQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var budgetResult = await this.VerifyBudgetAsync(budgetId, cancellationToken);
        if (!budgetResult.IsSuccess)
        {
            return Result<CursorPaginatedList<BudgetExpenseDto, int>>.Failure(budgetResult.ErrorType!.Value, budgetResult.ErrorMessage!);
        }

        queryParameters.BudgetId = budgetId;
        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.expenseRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(BudgetExpenseDto.FromEntity).ToList();

        var result = new CursorPaginatedList<BudgetExpenseDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);

        return Result<CursorPaginatedList<BudgetExpenseDto, int>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<BudgetExpenseDto>> GetExpenseByIdAsync(int budgetId, int expenseId, CancellationToken cancellationToken)
    {
        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(expense, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetExpenseDto>.Failure(notFound.Value, error);
        }

        return Result<BudgetExpenseDto>.Success(BudgetExpenseDto.FromEntity(expense!));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetExpenseDto>> CreateExpenseAsync(int budgetId, CreateBudgetExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var budgetResult = await this.VerifyBudgetAsync(budgetId, cancellationToken);
        if (!budgetResult.IsSuccess)
        {
            return Result<BudgetExpenseDto>.Failure(budgetResult.ErrorType!.Value, budgetResult.ErrorMessage!);
        }

        var budget = budgetResult.ValueOrThrow;

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<BudgetExpenseDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, budget.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<BudgetExpenseDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var expense = new BudgetExpense
        {
            UserId = budget.UserId,
            BudgetId = budget.Id,
            Name = request.Name,
            Description = request.Description,
            Amount = request.Amount,
            Cadence = request.Cadence,
            CategoryId = request.CategoryId,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId,
            BudgetExpenseTags = tagIds.Select(tagId => new BudgetExpenseTag { TagId = tagId }).ToList()
        };

        this.expenseRepository.Add(expense);
        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created budget expense {ExpenseId} in budget {BudgetId} for user {UserId}", expense.Id, budget.Id, budget.UserId);

        return Result<BudgetExpenseDto>.Success(BudgetExpenseDto.FromEntity(expense));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetExpenseDto>> UpdateExpenseAsync(int budgetId, int expenseId, UpdateBudgetExpenseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(expense, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetExpenseDto>.Failure(notFound.Value, error);
        }

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<BudgetExpenseDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, expense!.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<BudgetExpenseDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        expense.Name = request.Name;
        expense.Description = request.Description;
        expense.Amount = request.Amount;
        expense.Cadence = request.Cadence;
        expense.CategoryId = request.CategoryId;
        expense.Metadata = request.Metadata ?? [];
        expense.UpdatedById = this.currentUserService.UserId;

        SyncTags(expense, tagIds);

        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        return Result<BudgetExpenseDto>.Success(BudgetExpenseDto.FromEntity(expense));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetExpenseDto>> PatchExpenseAsync(int budgetId, int expenseId, JsonPatchDocument<UpdateBudgetExpenseRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<BudgetExpenseDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(expense, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetExpenseDto>.Failure(notFound.Value, error);
        }

        var request = UpdateBudgetExpenseRequest.FromEntity(expense!);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<BudgetExpenseDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<BudgetExpenseDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateExpenseAsync(budgetId, expenseId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteExpenseAsync(int budgetId, int expenseId, CancellationToken cancellationToken)
    {
        var expense = await this.expenseRepository.GetByIdAsync(expenseId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(expense, budgetId, out var error);
        if (notFound != null)
        {
            return Result<bool>.Failure(notFound.Value, error);
        }

        this.expenseRepository.Remove(expense!);
        await this.expenseRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted budget expense {ExpenseId} from budget {BudgetId} for user {UserId}", expenseId, budgetId, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    private static List<int> NormalizeTagIds(IReadOnlyList<int>? tagIds)
    {
        return tagIds == null ? [] : tagIds.Distinct().ToList();
    }

    private static void SyncTags(BudgetExpense expense, List<int> desiredTagIds)
    {
        var desired = desiredTagIds.ToHashSet();
        var existing = expense.BudgetExpenseTags.Select(et => et.TagId).ToHashSet();

        expense.BudgetExpenseTags.RemoveAll(et => !desired.Contains(et.TagId));

        foreach (var tagId in desired.Where(tagId => !existing.Contains(tagId)))
        {
            expense.BudgetExpenseTags.Add(new BudgetExpenseTag { BudgetExpenseId = expense.Id, TagId = tagId });
        }
    }

    /// <summary>
    /// Validates that a line item exists, belongs to the given budget, and is accessible. Returns the
    /// failure error type (with message) when invalid, or <c>null</c> when valid.
    /// </summary>
    private DomainErrorType? ResolveLineItem(BudgetExpense? expense, int budgetId, out string error)
    {
        if (expense == null || expense.BudgetId != budgetId)
        {
            error = "Expense line not found";
            return DomainErrorType.NotFound;
        }

        if (expense.UserId != this.currentUserService.UserId && !this.currentUserService.IsAdmin)
        {
            this.logger.LogWarning("User {UserId} attempted to access budget expense {ExpenseId} owned by {OwnerId}", this.currentUserService.UserId, expense.Id, expense.UserId);
            error = "You can only access your own budget expenses";
            return DomainErrorType.Forbidden;
        }

        error = string.Empty;
        return null;
    }

    private async Task<Result<Budget>> VerifyBudgetAsync(int budgetId, CancellationToken cancellationToken)
    {
        var budget = await this.budgetRepository.GetByIdAsync(budgetId, track: false, cancellationToken);

        if (budget == null)
        {
            return Result<Budget>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (budget.UserId != this.currentUserService.UserId && !this.currentUserService.IsAdmin)
        {
            this.logger.LogWarning("User {UserId} attempted to access budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, budgetId, budget.UserId);
            return Result<Budget>.Failure(DomainErrorType.Forbidden, "You can only access your own budgets");
        }

        return Result<Budget>.Success(budget);
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
