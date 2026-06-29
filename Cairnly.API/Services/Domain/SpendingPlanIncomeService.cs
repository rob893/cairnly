using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
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
public sealed class SpendingPlanIncomeService : SpendingPlanLineItemService<SpendingPlanIncome, SpendingPlanIncomeDto, CreateSpendingPlanIncomeRequest, UpdateSpendingPlanIncomeRequest, SpendingPlanIncomeQueryParameters, ISpendingPlanIncomeRepository>, ISpendingPlanIncomeService
{
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
        : base(logger, incomeRepository, spendingPlanRepository, categoryTagValidator, currentUserService)
    {
    }

    /// <inheritdoc />
    protected override string LineItemName => "income";

    /// <inheritdoc />
    protected override string LineItemNotFoundMessage => "Income line not found";

    /// <inheritdoc />
    protected override string LineItemForbiddenMessage => "You can only access your own spending plan income";

    /// <inheritdoc />
    public Task<Result<CursorPaginatedList<SpendingPlanIncomeDto, int>>> GetIncomesAsync(int spendingPlanId, SpendingPlanIncomeQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        return this.GetLineItemsAsync(spendingPlanId, queryParameters, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanIncomeDto>> GetIncomeByIdAsync(int spendingPlanId, int incomeId, CancellationToken cancellationToken)
    {
        return this.GetLineItemByIdAsync(spendingPlanId, incomeId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanIncomeDto>> CreateIncomeAsync(int spendingPlanId, CreateSpendingPlanIncomeRequest request, CancellationToken cancellationToken)
    {
        return this.CreateLineItemAsync(spendingPlanId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanIncomeDto>> UpdateIncomeAsync(int spendingPlanId, int incomeId, UpdateSpendingPlanIncomeRequest request, CancellationToken cancellationToken)
    {
        return this.UpdateLineItemAsync(spendingPlanId, incomeId, request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<SpendingPlanIncomeDto>> PatchIncomeAsync(int spendingPlanId, int incomeId, JsonPatchDocument<UpdateSpendingPlanIncomeRequest> patchDocument, CancellationToken cancellationToken)
    {
        return this.PatchLineItemAsync(spendingPlanId, incomeId, patchDocument, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<bool>> DeleteIncomeAsync(int spendingPlanId, int incomeId, CancellationToken cancellationToken)
    {
        return this.DeleteLineItemAsync(spendingPlanId, incomeId, cancellationToken);
    }

    /// <inheritdoc />
    protected override SpendingPlanIncomeDto ToDto(SpendingPlanIncome lineItem)
    {
        return SpendingPlanIncomeDto.FromEntity(lineItem);
    }

    /// <inheritdoc />
    protected override UpdateSpendingPlanIncomeRequest ToUpdateRequest(SpendingPlanIncome lineItem)
    {
        return UpdateSpendingPlanIncomeRequest.FromEntity(lineItem);
    }

    /// <inheritdoc />
    protected override void SetTags(SpendingPlanIncome lineItem, IReadOnlyCollection<int> tagIds)
    {
        lineItem.SpendingPlanIncomeTags = tagIds.Select(tagId => new SpendingPlanIncomeTag { TagId = tagId }).ToList();
    }

    /// <inheritdoc />
    protected override void SyncTags(SpendingPlanIncome lineItem, IReadOnlyCollection<int> tagIds)
    {
        TagLinkUtilities.Sync(lineItem.SpendingPlanIncomeTags, tagIds, tagId => new SpendingPlanIncomeTag { SpendingPlanIncomeId = lineItem.Id, TagId = tagId });
    }
}