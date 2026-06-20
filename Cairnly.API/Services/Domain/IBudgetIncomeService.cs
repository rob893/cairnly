using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.BudgetIncomes;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for managing a budget's income line items.
/// </summary>
public interface IBudgetIncomeService
{
    /// <summary>
    /// Gets a cursor-paginated list of income line items for a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of income DTOs, or a failure if the budget is not accessible.</returns>
    Task<Result<CursorPaginatedList<BudgetIncomeDto, int>>> GetIncomesAsync(int budgetId, BudgetIncomeQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The income DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<BudgetIncomeDto>> GetIncomeByIdAsync(int budgetId, int incomeId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new income line item in a budget.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created income DTO.</returns>
    Task<Result<BudgetIncomeDto>> CreateIncomeAsync(int budgetId, CreateBudgetIncomeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated income DTO.</returns>
    Task<Result<BudgetIncomeDto>> UpdateIncomeAsync(int budgetId, int incomeId, UpdateBudgetIncomeRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing income line item via a JSON Patch document.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated income DTO.</returns>
    Task<Result<BudgetIncomeDto>> PatchIncomeAsync(int budgetId, int incomeId, JsonPatchDocument<UpdateBudgetIncomeRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an income line item.
    /// </summary>
    /// <param name="budgetId">The parent budget ID.</param>
    /// <param name="incomeId">The income line ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteIncomeAsync(int budgetId, int incomeId, CancellationToken cancellationToken);
}
