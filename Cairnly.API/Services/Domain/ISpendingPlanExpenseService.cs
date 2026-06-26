using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlanExpenses;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for managing a spendingPlan's expense line items.
/// </summary>
public interface ISpendingPlanExpenseService
{
    /// <summary>
    /// Gets a cursor-paginated list of expense line items for a spendingPlan.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of expense DTOs, or a failure if the spendingPlan is not accessible.</returns>
    Task<Result<CursorPaginatedList<SpendingPlanExpenseDto, int>>> GetExpensesAsync(int spendingPlanId, SpendingPlanExpenseQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single expense line item.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The expense DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<SpendingPlanExpenseDto>> GetExpenseByIdAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new expense line item in a spendingPlan.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created expense DTO.</returns>
    Task<Result<SpendingPlanExpenseDto>> CreateExpenseAsync(int spendingPlanId, CreateSpendingPlanExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing expense line item.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated expense DTO.</returns>
    Task<Result<SpendingPlanExpenseDto>> UpdateExpenseAsync(int spendingPlanId, int expenseId, UpdateSpendingPlanExpenseRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing expense line item via a JSON Patch document.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated expense DTO.</returns>
    Task<Result<SpendingPlanExpenseDto>> PatchExpenseAsync(int spendingPlanId, int expenseId, JsonPatchDocument<UpdateSpendingPlanExpenseRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an expense line item.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="expenseId">The expense line ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteExpenseAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken);
}