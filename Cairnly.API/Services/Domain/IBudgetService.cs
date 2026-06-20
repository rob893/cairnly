using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Budgets;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for budget management.
/// </summary>
public interface IBudgetService
{
    /// <summary>
    /// Gets a cursor-paginated list of budgets for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of budget DTOs.</returns>
    Task<CursorPaginatedList<BudgetDto, int>> GetBudgetsAsync(BudgetQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single budget by ID.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The budget DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<BudgetDto>> GetBudgetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the computed summary for a budget.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The budget summary if found and accessible; otherwise a failure result.</returns>
    Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new budget for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created budget DTO.</returns>
    Task<Result<BudgetDto>> CreateBudgetAsync(CreateBudgetRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing budget.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated budget DTO.</returns>
    Task<Result<BudgetDto>> UpdateBudgetAsync(int id, UpdateBudgetRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing budget via a JSON Patch document.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated budget DTO.</returns>
    Task<Result<BudgetDto>> PatchBudgetAsync(int id, JsonPatchDocument<UpdateBudgetRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a budget and its line items.
    /// </summary>
    /// <param name="id">The budget ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteBudgetAsync(int id, CancellationToken cancellationToken);
}
