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
using Cairnly.API.Models.Requests.SpendingPlanIncomes;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for managing a spendingPlan's income line items.
/// </summary>
public sealed class SpendingPlanIncomeService : ISpendingPlanIncomeService
{
    private readonly ILogger<SpendingPlanIncomeService> logger;

    private readonly ISpendingPlanIncomeRepository incomeRepository;

    private readonly ISpendingPlanRepository spendingPlanRepository;

    private readonly ICategoryTagValidator categoryTagValidator;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanIncomeService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="incomeRepository">The spendingPlan income repository.</param>
    /// <param name="spendingPlanRepository">The spendingPlan repository.</param>
    /// <param name="categoryTagValidator">The category/tag validator.</param>
    /// <param name="currentUserService">The current user service.</param>
    public SpendingPlanIncomeService(
        ILogger<SpendingPlanIncomeService> logger,
        ISpendingPlanIncomeRepository incomeRepository,
        ISpendingPlanRepository spendingPlanRepository,
        ICategoryTagValidator categoryTagValidator,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
        this.spendingPlanRepository = spendingPlanRepository ?? throw new ArgumentNullException(nameof(spendingPlanRepository));
        this.categoryTagValidator = categoryTagValidator ?? throw new ArgumentNullException(nameof(categoryTagValidator));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<CursorPaginatedList<SpendingPlanIncomeDto, int>>> GetIncomesAsync(int spendingPlanId, SpendingPlanIncomeQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<CursorPaginatedList<SpendingPlanIncomeDto, int>>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        queryParameters.SpendingPlanId = spendingPlanId;
        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.incomeRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(SpendingPlanIncomeDto.FromEntity).ToList();

        var result = new CursorPaginatedList<SpendingPlanIncomeDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);

        return Result<CursorPaginatedList<SpendingPlanIncomeDto, int>>.Success(result);
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanIncomeDto>> GetIncomeByIdAsync(int spendingPlanId, int incomeId, CancellationToken cancellationToken)
    {
        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(income, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanIncomeDto>.Failure(notFound.Value, error);
        }

        return Result<SpendingPlanIncomeDto>.Success(SpendingPlanIncomeDto.FromEntity(income!));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanIncomeDto>> CreateIncomeAsync(int spendingPlanId, CreateSpendingPlanIncomeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<SpendingPlanIncomeDto>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        var spendingPlan = spendingPlanResult.ValueOrThrow;

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<SpendingPlanIncomeDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, spendingPlan.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<SpendingPlanIncomeDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var income = new SpendingPlanIncome
        {
            UserId = spendingPlan.UserId,
            SpendingPlanId = spendingPlan.Id,
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Amount = request.Amount,
            Cadence = request.Cadence,
            CategoryId = request.CategoryId,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId,
            SpendingPlanIncomeTags = tagIds.Select(tagId => new SpendingPlanIncomeTag { TagId = tagId }).ToList()
        };

        this.incomeRepository.Add(income);
        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created spendingPlan income {IncomeId} in spendingPlan {SpendingPlanId} for user {UserId}", income.Id, spendingPlan.Id, spendingPlan.UserId);

        return Result<SpendingPlanIncomeDto>.Success(SpendingPlanIncomeDto.FromEntity(income));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanIncomeDto>> UpdateIncomeAsync(int spendingPlanId, int incomeId, UpdateSpendingPlanIncomeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(income, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanIncomeDto>.Failure(notFound.Value, error);
        }

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<SpendingPlanIncomeDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, income!.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<SpendingPlanIncomeDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        income.Name = request.Name;
        income.Description = request.Description;
        income.Type = request.Type;
        income.Amount = request.Amount;
        income.Cadence = request.Cadence;
        income.CategoryId = request.CategoryId;
        income.Metadata = request.Metadata ?? [];
        income.UpdatedById = this.currentUserService.UserId;

        TagLinkUtilities.Sync(income.SpendingPlanIncomeTags, tagIds, tagId => new SpendingPlanIncomeTag { SpendingPlanIncomeId = income.Id, TagId = tagId });

        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        return Result<SpendingPlanIncomeDto>.Success(SpendingPlanIncomeDto.FromEntity(income));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanIncomeDto>> PatchIncomeAsync(int spendingPlanId, int incomeId, JsonPatchDocument<UpdateSpendingPlanIncomeRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<SpendingPlanIncomeDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(income, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<SpendingPlanIncomeDto>.Failure(notFound.Value, error);
        }

        var request = UpdateSpendingPlanIncomeRequest.FromEntity(income!);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<SpendingPlanIncomeDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<SpendingPlanIncomeDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateIncomeAsync(spendingPlanId, incomeId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteIncomeAsync(int spendingPlanId, int incomeId, CancellationToken cancellationToken)
    {
        var income = await this.incomeRepository.GetByIdAsync(incomeId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(income, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<bool>.Failure(notFound.Value, error);
        }

        this.incomeRepository.Remove(income!);
        await this.incomeRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted spendingPlan income {IncomeId} from spendingPlan {SpendingPlanId} for user {UserId}", incomeId, spendingPlanId, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Validates that a line item exists, belongs to the given spendingPlan, and is accessible. Returns the
    /// failure error type (with message) when invalid, or <c>null</c> when valid.
    /// </summary>
    private DomainErrorType? ResolveLineItem(SpendingPlanIncome? income, int spendingPlanId, out string error)
    {
        if (income == null || income.SpendingPlanId != spendingPlanId)
        {
            error = "Income line not found";
            return DomainErrorType.NotFound;
        }

        if (!this.currentUserService.IsUserAuthorizedForResource(income))
        {
            this.logger.LogWarning("User {UserId} attempted to access spendingPlan income {IncomeId} owned by {OwnerId}", this.currentUserService.UserId, income.Id, income.UserId);
            error = "You can only access your own spending plan income";
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
