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
using Cairnly.API.Models.Requests.Budgets;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for budget management, including the computed budget summary.
/// </summary>
public sealed class BudgetService : IBudgetService
{
    private readonly ILogger<BudgetService> logger;

    private readonly IBudgetRepository budgetRepository;

    private readonly IBudgetIncomeRepository incomeRepository;

    private readonly IBudgetExpenseRepository expenseRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BudgetService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="budgetRepository">The budget repository.</param>
    /// <param name="incomeRepository">The budget income repository.</param>
    /// <param name="expenseRepository">The budget expense repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public BudgetService(
        ILogger<BudgetService> logger,
        IBudgetRepository budgetRepository,
        IBudgetIncomeRepository incomeRepository,
        IBudgetExpenseRepository expenseRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.budgetRepository = budgetRepository ?? throw new ArgumentNullException(nameof(budgetRepository));
        this.incomeRepository = incomeRepository ?? throw new ArgumentNullException(nameof(incomeRepository));
        this.expenseRepository = expenseRepository ?? throw new ArgumentNullException(nameof(expenseRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<BudgetDto, int>> GetBudgetsAsync(BudgetQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.budgetRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(BudgetDto.FromEntity).ToList();

        return new CursorPaginatedList<BudgetDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<BudgetDto>> GetBudgetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var budget = await this.budgetRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (budget == null)
        {
            return Result<BudgetDto>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (!this.CanAccess(budget))
        {
            this.logger.LogWarning("User {UserId} attempted to access budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, id, budget.UserId);
            return Result<BudgetDto>.Failure(DomainErrorType.Forbidden, "You can only access your own budgets");
        }

        return Result<BudgetDto>.Success(BudgetDto.FromEntity(budget));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync(int id, CancellationToken cancellationToken)
    {
        var budget = await this.budgetRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (budget == null)
        {
            return Result<BudgetSummaryDto>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (!this.CanAccess(budget))
        {
            this.logger.LogWarning("User {UserId} attempted to access summary for budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, id, budget.UserId);
            return Result<BudgetSummaryDto>.Failure(DomainErrorType.Forbidden, "You can only access your own budgets");
        }

        var incomes = await this.incomeRepository.SearchAsync(i => i.BudgetId == id, track: false, cancellationToken);
        var expenses = await this.expenseRepository.SearchAsync(e => e.BudgetId == id, track: false, cancellationToken);

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

        return Result<BudgetSummaryDto>.Success(new BudgetSummaryDto
        {
            BudgetId = budget.Id,
            Currency = budget.Currency,
            Income = income,
            Expenses = expense,
            Remaining = remaining
        });
    }

    /// <inheritdoc />
    public async Task<Result<BudgetDto>> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var budget = new Budget
        {
            UserId = this.currentUserService.UserId,
            Name = request.Name,
            Description = request.Description,
            Currency = request.Currency.ToUpperInvariant(),
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.budgetRepository.Add(budget);
        await this.budgetRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created budget {BudgetId} for user {UserId}", budget.Id, budget.UserId);

        return Result<BudgetDto>.Success(BudgetDto.FromEntity(budget));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetDto>> UpdateBudgetAsync(int id, UpdateBudgetRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var budget = await this.budgetRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (budget == null)
        {
            return Result<BudgetDto>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (!this.CanAccess(budget))
        {
            this.logger.LogWarning("User {UserId} attempted to update budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, id, budget.UserId);
            return Result<BudgetDto>.Failure(DomainErrorType.Forbidden, "You can only update your own budgets");
        }

        budget.Name = request.Name;
        budget.Description = request.Description;
        budget.Currency = request.Currency.ToUpperInvariant();
        budget.Metadata = request.Metadata ?? [];
        budget.UpdatedById = this.currentUserService.UserId;

        await this.budgetRepository.SaveChangesAsync(cancellationToken);

        return Result<BudgetDto>.Success(BudgetDto.FromEntity(budget));
    }

    /// <inheritdoc />
    public async Task<Result<BudgetDto>> PatchBudgetAsync(int id, JsonPatchDocument<UpdateBudgetRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<BudgetDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var budget = await this.budgetRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (budget == null)
        {
            return Result<BudgetDto>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (!this.CanAccess(budget))
        {
            this.logger.LogWarning("User {UserId} attempted to patch budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, id, budget.UserId);
            return Result<BudgetDto>.Failure(DomainErrorType.Forbidden, "You can only update your own budgets");
        }

        var request = UpdateBudgetRequest.FromEntity(budget);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<BudgetDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<BudgetDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateBudgetAsync(id, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteBudgetAsync(int id, CancellationToken cancellationToken)
    {
        var budget = await this.budgetRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (budget == null)
        {
            return Result<bool>.Failure(DomainErrorType.NotFound, "Budget not found");
        }

        if (!this.CanAccess(budget))
        {
            this.logger.LogWarning("User {UserId} attempted to delete budget {BudgetId} owned by {OwnerId}", this.currentUserService.UserId, id, budget.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own budgets");
        }

        this.budgetRepository.Remove(budget);
        await this.budgetRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted budget {BudgetId} for user {UserId}", id, this.currentUserService.UserId);

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

    private bool CanAccess(Budget budget)
    {
        return budget.UserId == this.currentUserService.UserId || this.currentUserService.IsAdmin;
    }
}
