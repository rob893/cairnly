using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlans;
using Microsoft.AspNetCore.JsonPatch;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service interface for spendingPlan management.
/// </summary>
public interface ISpendingPlanService
{
    /// <summary>
    /// Gets a cursor-paginated list of spendingPlans for the current user.
    /// </summary>
    /// <param name="queryParameters">The pagination and filter parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A paginated list of spendingPlan DTOs.</returns>
    Task<CursorPaginatedList<SpendingPlanDto, int>> GetSpendingPlansAsync(SpendingPlanQueryParameters queryParameters, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a single spendingPlan by ID.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The spendingPlan DTO if found and accessible; otherwise a failure result.</returns>
    Task<Result<SpendingPlanDto>> GetSpendingPlanByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the computed summary for a spendingPlan.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The spendingPlan summary if found and accessible; otherwise a failure result.</returns>
    Task<Result<SpendingPlanSummaryDto>> GetSpendingPlanSummaryAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Gets computed summaries for all spendingPlans accessible to the current user.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The spendingPlan summaries for the current user.</returns>
    Task<Result<IReadOnlyList<SpendingPlanSummaryDto>>> GetSpendingPlanSummariesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new spendingPlan for the current user.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created spendingPlan DTO.</returns>
    Task<Result<SpendingPlanDto>> CreateSpendingPlanAsync(CreateSpendingPlanRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing spendingPlan.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated spendingPlan DTO.</returns>
    Task<Result<SpendingPlanDto>> UpdateSpendingPlanAsync(int id, UpdateSpendingPlanRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Partially updates an existing spendingPlan via a JSON Patch document.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The updated spendingPlan DTO.</returns>
    Task<Result<SpendingPlanDto>> PatchSpendingPlanAsync(int id, JsonPatchDocument<UpdateSpendingPlanRequest> patchDocument, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a spendingPlan and its line items.
    /// </summary>
    /// <param name="id">The spendingPlan ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<bool>> DeleteSpendingPlanAsync(int id, CancellationToken cancellationToken);
}