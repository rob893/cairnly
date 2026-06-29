using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
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
public sealed class SpendingPlanExpenseService : SpendingPlanLineItemService<SpendingPlanExpense, SpendingPlanExpenseDto, CreateSpendingPlanExpenseRequest, UpdateSpendingPlanExpenseRequest, SpendingPlanExpenseQueryParameters, ISpendingPlanExpenseRepository>, ISpendingPlanExpenseService
{
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
        : base(logger, expenseRepository, spendingPlanRepository, categoryTagValidator, currentUserService)
    {
    }

    /// <inheritdoc />
    protected override string LineItemName => "expense";

    /// <inheritdoc />
    protected override string LineItemNotFoundMessage => "Expense line not found";

    /// <inheritdoc />
    protected override string LineItemForbiddenMessage => "You can only access your own spending plan expenses";

    /// <inheritdoc />
    public Task<Result<CursorPaginatedList<SpendingPlanExpenseDto, int>>> GetExpensesAsync(int spendingPlanId, SpendingPlanExpenseQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        return this.GetLineItemsAsync(spendingPlanId, queryParameters, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanExpenseDto>> GetExpenseByIdAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken)
    {
        return this.GetLineItemByIdAsync(spendingPlanId, expenseId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanExpenseDto>> CreateExpenseAsync(int spendingPlanId, CreateSpendingPlanExpenseRequest request, CancellationToken cancellationToken)
    {
        return this.CreateLineItemAsync(spendingPlanId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanExpenseDto>> UpdateExpenseAsync(int spendingPlanId, int expenseId, UpdateSpendingPlanExpenseRequest request, CancellationToken cancellationToken)
    {
        return this.UpdateLineItemAsync(spendingPlanId, expenseId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanExpenseDto>> PatchExpenseAsync(int spendingPlanId, int expenseId, JsonPatchDocument<UpdateSpendingPlanExpenseRequest> patchDocument, CancellationToken cancellationToken)
    {
        return this.PatchLineItemAsync(spendingPlanId, expenseId, patchDocument, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<bool>> DeleteExpenseAsync(int spendingPlanId, int expenseId, CancellationToken cancellationToken)
    {
        return this.DeleteLineItemAsync(spendingPlanId, expenseId, cancellationToken);
    }

    /// <inheritdoc />
    protected override SpendingPlanExpenseDto ToDto(SpendingPlanExpense lineItem)
    {
        return SpendingPlanExpenseDto.FromEntity(lineItem);
    }

    /// <inheritdoc />
    protected override UpdateSpendingPlanExpenseRequest ToUpdateRequest(SpendingPlanExpense lineItem)
    {
        return UpdateSpendingPlanExpenseRequest.FromEntity(lineItem);
    }

    /// <inheritdoc />
    protected override void SetTags(SpendingPlanExpense lineItem, IReadOnlyCollection<int> tagIds)
    {
        lineItem.SpendingPlanExpenseTags = tagIds.Select(tagId => new SpendingPlanExpenseTag { TagId = tagId }).ToList();
    }

    /// <inheritdoc />
    protected override void SyncTags(SpendingPlanExpense lineItem, IReadOnlyCollection<int> tagIds)
    {
        TagLinkUtilities.Sync(lineItem.SpendingPlanExpenseTags, tagIds, tagId => new SpendingPlanExpenseTag { SpendingPlanExpenseId = lineItem.Id, TagId = tagId });
    }
}