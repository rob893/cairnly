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
using Cairnly.API.Models.Requests.BudgetIncomes;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for managing a budget's income line items.
/// </summary>
public sealed class BudgetIncomeService : IBudgetIncomeService
{
    private readonly ILogger<BudgetIncomeService> logger;

    private readonly IBudgetIncomeRepository incomeRepository;

    private readonly IBudgetRepository budgetRepository;

    private readonly ICategoryRepository categoryRepository;

    private readonly ITagRepository tagRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetIncomeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="incomeRepository">The budget income repository.</param>
    /// <param name="budgetRepository">The budget repository.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public BudgetIncomeService(
        ILogger<BudgetIncomeService> logger,
        IBudgetIncomeRepository incomeRepository,
        IBudgetRepository budgetRepository,
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
        this.budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<CursorPaginatedList<BudgetIncomeDto, int>>> GetIncomesAsync(int budgetId, BudgetIncomeQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var budgetResult = await this.VerifyBudgetAsync(budgetId, cancellationToken);
        if (!budgetResult.IsSuccess)
        {
            return Result<CursorPaginatedList<BudgetIncomeDto, int>>.Failure(budgetResult.ErrorType!.Value, budgetResult.ErrorMessage!);
        }

        queryParameters.BudgetId = budgetId;
        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.incomeRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(BudgetIncomeDto.FromEntity).ToList();

        var result = new CursorPaginatedList<BudgetIncomeDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);

        return Result<CursorPaginatedList<BudgetIncomeDto, int>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<BudgetIncomeDto>> GetIncomeByIdAsync(int budgetId, int incomeId, CancellationToken cancellationToken)
    {
        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(income, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetIncomeDto>.Failure(notFound.Value, error);
        }

        return Result<BudgetIncomeDto>.Success(BudgetIncomeDto.FromEntity(income!));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetIncomeDto>> CreateIncomeAsync(int budgetId, CreateBudgetIncomeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var budgetResult = await this.VerifyBudgetAsync(budgetId, cancellationToken);
        if (!budgetResult.IsSuccess)
        {
            return Result<BudgetIncomeDto>.Failure(budgetResult.ErrorType!.Value, budgetResult.ErrorMessage!);
        }

        var budget = budgetResult.ValueOrThrow;

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<BudgetIncomeDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, budget.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<BudgetIncomeDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var income = new BudgetIncome
        {
            UserId = budget.UserId,
            BudgetId = budget.Id,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Amount = request.Amount,
            Cadence = request.Cadence,
            CategoryId = request.CategoryId,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId,
            BudgetIncomeTags = tagIds.Select(tagId => new BudgetIncomeTag { TagId = tagId }).ToList()
        };

        this.incomeRepository.Add(income);
        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created budget income {IncomeId} in budget {BudgetId} for user {UserId}", income.Id, budget.Id, budget.UserId);

        return Result<BudgetIncomeDto>.Success(BudgetIncomeDto.FromEntity(income));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetIncomeDto>> UpdateIncomeAsync(int budgetId, int incomeId, UpdateBudgetIncomeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(income, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetIncomeDto>.Failure(notFound.Value, error);
        }

        var categoryResult = await this.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<BudgetIncomeDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = NormalizeTagIds(request.TagIds);
        var tagResult = await this.ValidateTagsAsync(tagIds, income!.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<BudgetIncomeDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        income.Name = request.Name;
        income.Description = request.Description;
        income.Type = request.Type;
        income.Amount = request.Amount;
        income.Cadence = request.Cadence;
        income.CategoryId = request.CategoryId;
        income.Metadata = request.Metadata ?? [];
        income.UpdatedById = this.currentUserService.UserId;

        SyncTags(income, tagIds);

        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        return Result<BudgetIncomeDto>.Success(BudgetIncomeDto.FromEntity(income));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetIncomeDto>> PatchIncomeAsync(int budgetId, int incomeId, JsonPatchDocument<UpdateBudgetIncomeRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<BudgetIncomeDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(income, budgetId, out var error);
        if (notFound != null)
        {
            return Result<BudgetIncomeDto>.Failure(notFound.Value, error);
        }

        var request = UpdateBudgetIncomeRequest.FromEntity(income!);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<BudgetIncomeDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<BudgetIncomeDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateIncomeAsync(budgetId, incomeId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteIncomeAsync(int budgetId, int incomeId, CancellationToken cancellationToken)
    {
        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(income, budgetId, out var error);
        if (notFound != null)
        {
            return Result<bool>.Failure(notFound.Value, error);
        }

        this.incomeRepository.Remove(income!);
        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted budget income {IncomeId} from budget {BudgetId} for user {UserId}", incomeId, budgetId, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    private static List<int> NormalizeTagIds(IReadOnlyList<int>? tagIds)
    {
        return tagIds == null ? [] : tagIds.Distinct().ToList();
    }

    private static void SyncTags(BudgetIncome income, List<int> desiredTagIds)
    {
        var desired = desiredTagIds.ToHashSet();
        var existing = income.BudgetIncomeTags.Select(it => it.TagId).ToHashSet();

        income.BudgetIncomeTags.RemoveAll(it => !desired.Contains(it.TagId));

        foreach (var tagId in desired.Where(tagId => !existing.Contains(tagId)))
        {
            income.BudgetIncomeTags.Add(new BudgetIncomeTag { BudgetIncomeId = income.Id, TagId = tagId });
        }
    }

    /// <summary>
    /// Validates that a line item exists, belongs to the given budget, and is accessible. Returns the
    /// failure error type (with message) when invalid, or <c>null</c> when valid.
    /// </summary>
    private DomainErrorType? ResolveLineItem(BudgetIncome? income, int budgetId, out string error)
    {
        if (income == null || income.BudgetId != budgetId)
        {
            error = "Income line not found";
            return DomainErrorType.NotFound;
        }

        if (income.UserId != this.currentUserService.UserId && !this.currentUserService.IsAdmin)
        {
            this.logger.LogWarning("User {UserId} attempted to access budget income {IncomeId} owned by {OwnerId}", this.currentUserService.UserId, income.Id, income.UserId);
            error = "You can only access your own budget income";
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
