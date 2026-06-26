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
using Cairnly.API.Models.Requests.SpendingPlans;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for spendingPlan management, including the computed spendingPlan summary.
/// </summary>
public sealed class SpendingPlanService : ISpendingPlanService
{
    private readonly ILogger<SpendingPlanService> logger;

    private readonly ISpendingPlanRepository spendingPlanRepository;

    private readonly ISpendingPlanIncomeRepository incomeRepository;

    private readonly ISpendingPlanExpenseRepository expenseRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="spendingPlanRepository">The spendingPlan repository.</param>
    /// <param name="incomeRepository">The spendingPlan income repository.</param>
    /// <param name="expenseRepository">The spendingPlan expense repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public SpendingPlanService(
        ILogger<SpendingPlanService> logger,
        ISpendingPlanRepository spendingPlanRepository,
        ISpendingPlanIncomeRepository incomeRepository,
        ISpendingPlanExpenseRepository expenseRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.spendingPlanRepository = spendingPlanRepository ?? throw new ArgumentNullException(nameof(spendingPlanRepository));
        this.incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
        this.expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<SpendingPlanDto, int>> GetSpendingPlansAsync(SpendingPlanQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.spendingPlanRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(SpendingPlanDto.FromEntity).ToList();

        return new CursorPaginatedList<SpendingPlanDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanDto>> GetSpendingPlanByIdAsync(int id, CancellationToken cancellationToken)
    {
        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.CanAccess(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to access spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, id, spendingPlan.UserId);
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Forbidden, "You can only access your own spending plans");
        }

        return Result<SpendingPlanDto>.Success(SpendingPlanDto.FromEntity(spendingPlan));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanSummaryDto>> GetSpendingPlanSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<SpendingPlanSummaryDto>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.CanAccess(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to access summary for spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, id, spendingPlan.UserId);
            return Result<SpendingPlanSummaryDto>.Failure(DomainErrorType.Forbidden, "You can only access your own spending plans");
        }

        var incomes = await this.incomeRepository.SearchAsync(i => i.SpendingPlanId == id, track: false, cancellationToken);
        var expenses = await this.expenseRepository.SearchAsync(e => e.SpendingPlanId == id, track: false, cancellationToken);

        var incomeAnnual = incomes.Sum(i => i.Amount * i.Cadence.PeriodsPerYear());
        var expenseAnnual = expenses.Sum(e => e.Amount * e.Cadence.PeriodsPerYear());

        var income = BuildCadenceAmounts(incomeAnnual);
        var expense = BuildCadenceAmounts(expenseAnnual);
        var remaining = new CadenceAmountsDto
        {
            Daily = income.Daily - expense.Daily,
            Weekly = income.Weekly - expense.Weekly,
            Monthly = income.Monthly - expense.Monthly,
            Annual = income.Annual - expense.Annual
        };

        return Result<SpendingPlanSummaryDto>.Success(new SpendingPlanSummaryDto
        {
            SpendingPlanId = spendingPlan.Id,
            Currency = spendingPlan.Currency,
            Income = income,
            Expenses = expense,
            Remaining = remaining
        });
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanDto>> CreateSpendingPlanAsync(CreateSpendingPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spendingPlan = new SpendingPlan
        {
            UserId = this.currentUserService.UserId,
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency.ToUpperInvariant(),
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.spendingPlanRepository.Add(spendingPlan);
        await this.spendingPlanRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created spendingPlan {SpendingPlanId} for user {UserId}", spendingPlan.Id, spendingPlan.UserId);

        return Result<SpendingPlanDto>.Success(SpendingPlanDto.FromEntity(spendingPlan));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanDto>> UpdateSpendingPlanAsync(int id, UpdateSpendingPlanRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.CanAccess(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to update spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, id, spendingPlan.UserId);
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Forbidden, "You can only update your own spending plans");
        }

        spendingPlan.Name = request.Name;
        spendingPlan.Description = request.Description;
        spendingPlan.Currency = request.Currency.ToUpperInvariant();
        spendingPlan.Metadata = request.Metadata ?? [];
        spendingPlan.UpdatedById = this.currentUserService.UserId;

        await this.spendingPlanRepository.SaveChangesAsync(cancellationToken);

        return Result<SpendingPlanDto>.Success(SpendingPlanDto.FromEntity(spendingPlan));
    }

    /// <inheritdoc />
    public async Task<Result<SpendingPlanDto>> PatchSpendingPlanAsync(int id, JsonPatchDocument<UpdateSpendingPlanRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.CanAccess(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to patch spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, id, spendingPlan.UserId);
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Forbidden, "You can only update your own spending plans");
        }

        var request = UpdateSpendingPlanRequest.FromEntity(spendingPlan);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<SpendingPlanDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateSpendingPlanAsync(id, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteSpendingPlanAsync(int id, CancellationToken cancellationToken)
    {
        var spendingPlan = await this.spendingPlanRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (spendingPlan == null)
        {
            return Result<bool>.Failure(DomainErrorType.NotFound, "Spending plan not found");
        }

        if (!this.CanAccess(spendingPlan))
        {
            this.logger.LogWarning("User {UserId} attempted to delete spendingPlan {SpendingPlanId} owned by {OwnerId}", this.currentUserService.UserId, id, spendingPlan.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own spending plans");
        }

        this.spendingPlanRepository.Remove(spendingPlan);
        await this.spendingPlanRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted spendingPlan {SpendingPlanId} for user {UserId}", id, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    private static CadenceAmountsDto BuildCadenceAmounts(long annual)
    {
        decimal annualDecimal = annual;

        return new CadenceAmountsDto
        {
            Daily = (long)Math.Round(annualDecimal / 365m, MidpointRounding.AwayFromZero),
            Weekly = (long)Math.Round(annualDecimal / 52m, MidpointRounding.AwayFromZero),
            Monthly = (long)Math.Round(annualDecimal / 12m, MidpointRounding.AwayFromZero),
            Annual = annual
        };
    }

    private bool CanAccess(SpendingPlan spendingPlan)
    {
        return spendingPlan.UserId == this.currentUserService.UserId || this.currentUserService.IsAdmin;
    }
}
