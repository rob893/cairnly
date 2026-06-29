using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Base service for managing a spendingPlan's line items.
/// </summary>
/// <typeparam name="TEntity">The line item entity type.</typeparam>
/// <typeparam name="TDto">The line item DTO type.</typeparam>
/// <typeparam name="TCreateRequest">The create request type.</typeparam>
/// <typeparam name="TUpdateRequest">The update request type.</typeparam>
/// <typeparam name="TQueryParameters">The query parameter type.</typeparam>
/// <typeparam name="TRepository">The line item repository type.</typeparam>
public abstract class SpendingPlanLineItemService<TEntity, TDto, TCreateRequest, TUpdateRequest, TQueryParameters, TRepository>
    where TEntity : class, ISpendingPlanLineItem, new()
    where TDto : class
    where TCreateRequest : ISpendingPlanLineItemRequest
    where TUpdateRequest : class, ISpendingPlanLineItemRequest, new()
    where TQueryParameters : OwnedEntityQueryParameters, ISpendingPlanLineItemQueryParameters
    where TRepository : IRepository<TEntity, TQueryParameters>
{
    private readonly ILogger logger;

    private readonly TRepository lineItemRepository;

    private readonly ISpendingPlanRepository spendingPlanRepository;

    private readonly ICategoryTagValidator categoryTagValidator;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanLineItemService{TEntity,TDto,TCreateRequest,TUpdateRequest,TQueryParameters,TRepository}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="lineItemRepository">The line item repository.</param>
    /// <param name="spendingPlanRepository">The spendingPlan repository.</param>
    /// <param name="categoryTagValidator">The category/tag validator.</param>
    /// <param name="currentUserService">The current user service.</param>
    protected SpendingPlanLineItemService(
        ILogger logger,
        TRepository lineItemRepository,
        ISpendingPlanRepository spendingPlanRepository,
        ICategoryTagValidator categoryTagValidator,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.lineItemRepository = lineItemRepository ?? throw new ArgumentNullException(nameof(lineItemRepository));
        this.spendingPlanRepository = spendingPlanRepository ?? throw new ArgumentNullException(nameof(spendingPlanRepository));
        this.categoryTagValidator = categoryTagValidator ?? throw new ArgumentNullException(nameof(categoryTagValidator));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <summary>Gets the singular line item name used in logs.</summary>
    protected abstract string LineItemName { get; }

    /// <summary>Gets the error returned when the line item cannot be found.</summary>
    protected abstract string LineItemNotFoundMessage { get; }

    /// <summary>Gets the error returned when the line item belongs to another user.</summary>
    protected abstract string LineItemForbiddenMessage { get; }

    /// <summary>
    /// Gets a cursor-paginated list of line items for a spendingPlan.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="queryParameters">The pagination and filter query parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of DTOs, or a failure if the spendingPlan is not accessible.</returns>
    protected async Task<Result<CursorPaginatedList<TDto, int>>> GetLineItemsAsync(int spendingPlanId, TQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<CursorPaginatedList<TDto, int>>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        queryParameters.SpendingPlanId = spendingPlanId;
        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.lineItemRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList.Select(this.ToDto).ToList();

        var result = new CursorPaginatedList<TDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);

        return Result<CursorPaginatedList<TDto, int>>.Success(result);
    }

    /// <summary>
    /// Gets a single line item by ID.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="lineItemId">The line item ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The DTO if found and accessible; otherwise a failure result.</returns>
    protected async Task<Result<TDto>> GetLineItemByIdAsync(int spendingPlanId, int lineItemId, CancellationToken cancellationToken)
    {
        var lineItem = await this.lineItemRepository.GetByIdAsync(lineItemId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(lineItem, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<TDto>.Failure(notFound.Value, error);
        }

        return Result<TDto>.Success(this.ToDto(lineItem!));
    }

    /// <summary>
    /// Creates a new line item in a spendingPlan.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The created line item DTO.</returns>
    protected async Task<Result<TDto>> CreateLineItemAsync(int spendingPlanId, TCreateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spendingPlanResult = await this.VerifySpendingPlanAsync(spendingPlanId, cancellationToken);
        if (!spendingPlanResult.IsSuccess)
        {
            return Result<TDto>.Failure(spendingPlanResult.ErrorType!.Value, spendingPlanResult.ErrorMessage!);
        }

        var spendingPlan = spendingPlanResult.ValueOrThrow;

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<TDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, spendingPlan.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<TDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        var lineItem = new TEntity
        {
            UserId = spendingPlan.UserId,
            SpendingPlanId = spendingPlan.Id,
            Name = request.Name,
            Description = request.Description,
            Amount = request.Amount,
            Cadence = request.Cadence,
            CategoryId = request.CategoryId,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.SetTags(lineItem, tagIds);

        this.lineItemRepository.Add(lineItem);
        await this.lineItemRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created spendingPlan {LineItemName} {LineItemId} in spendingPlan {SpendingPlanId} for user {UserId}", this.LineItemName, lineItem.Id, spendingPlan.Id, spendingPlan.UserId);

        return Result<TDto>.Success(this.ToDto(lineItem));
    }

    /// <summary>
    /// Updates an existing line item.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="lineItemId">The line item ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated line item DTO.</returns>
    protected async Task<Result<TDto>> UpdateLineItemAsync(int spendingPlanId, int lineItemId, TUpdateRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lineItem = await this.lineItemRepository.GetByIdAsync(lineItemId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(lineItem, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<TDto>.Failure(notFound.Value, error);
        }

        var categoryResult = await this.categoryTagValidator.ValidateCategoryAsync(request.CategoryId, cancellationToken);
        if (!categoryResult.IsSuccess)
        {
            return Result<TDto>.Failure(categoryResult.ErrorType!.Value, categoryResult.ErrorMessage!);
        }

        var tagIds = TagLinkUtilities.Normalize(request.TagIds);
        var tagResult = await this.categoryTagValidator.ValidateTagsAsync(tagIds, lineItem!.UserId, cancellationToken);
        if (!tagResult.IsSuccess)
        {
            return Result<TDto>.Failure(tagResult.ErrorType!.Value, tagResult.ErrorMessage!);
        }

        lineItem.Name = request.Name;
        lineItem.Description = request.Description;
        lineItem.Amount = request.Amount;
        lineItem.Cadence = request.Cadence;
        lineItem.CategoryId = request.CategoryId;
        lineItem.Metadata = request.Metadata ?? [];
        lineItem.UpdatedById = this.currentUserService.UserId;

        this.SyncTags(lineItem, tagIds);

        await this.lineItemRepository.SaveChangesAsync(cancellationToken);

        return Result<TDto>.Success(this.ToDto(lineItem));
    }

    /// <summary>
    /// Partially updates an existing line item using a JSON Patch document.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="lineItemId">The line item ID.</param>
    /// <param name="patchDocument">The JSON Patch document over the update request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The updated line item DTO.</returns>
    protected async Task<Result<TDto>> PatchLineItemAsync(int spendingPlanId, int lineItemId, JsonPatchDocument<TUpdateRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<TDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var lineItem = await this.lineItemRepository.GetByIdAsync(lineItemId, track: false, cancellationToken);

        var notFound = this.ResolveLineItem(lineItem, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<TDto>.Failure(notFound.Value, error);
        }

        var request = this.ToUpdateRequest(lineItem!);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<TDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<TDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateLineItemAsync(spendingPlanId, lineItemId, request, cancellationToken);
    }

    /// <summary>
    /// Deletes an existing line item.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="lineItemId">The line item ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    protected async Task<Result<bool>> DeleteLineItemAsync(int spendingPlanId, int lineItemId, CancellationToken cancellationToken)
    {
        var lineItem = await this.lineItemRepository.GetByIdAsync(lineItemId, track: true, cancellationToken);

        var notFound = this.ResolveLineItem(lineItem, spendingPlanId, out var error);
        if (notFound != null)
        {
            return Result<bool>.Failure(notFound.Value, error);
        }

        this.lineItemRepository.Remove(lineItem!);
        await this.lineItemRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted spendingPlan {LineItemName} {LineItemId} from spendingPlan {SpendingPlanId} for user {UserId}", this.LineItemName, lineItemId, spendingPlanId, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Maps a line item entity to its DTO.
    /// </summary>
    /// <param name="lineItem">The line item entity.</param>
    /// <returns>The mapped DTO.</returns>
    protected abstract TDto ToDto(TEntity lineItem);

    /// <summary>
    /// Creates an update request from an existing line item.
    /// </summary>
    /// <param name="lineItem">The line item entity.</param>
    /// <returns>The update request.</returns>
    protected abstract TUpdateRequest ToUpdateRequest(TEntity lineItem);

    /// <summary>
    /// Sets the tag join rows for a new line item.
    /// </summary>
    /// <param name="lineItem">The new line item entity.</param>
    /// <param name="tagIds">The normalized tag IDs.</param>
    protected abstract void SetTags(TEntity lineItem, IReadOnlyCollection<int> tagIds);

    /// <summary>
    /// Synchronizes the tag join rows for an existing line item.
    /// </summary>
    /// <param name="lineItem">The existing line item entity.</param>
    /// <param name="tagIds">The normalized tag IDs.</param>
    protected abstract void SyncTags(TEntity lineItem, IReadOnlyCollection<int> tagIds);

    private DomainErrorType? ResolveLineItem(TEntity? lineItem, int spendingPlanId, out string error)
    {
        if (lineItem == null || lineItem.SpendingPlanId != spendingPlanId)
        {
            error = this.LineItemNotFoundMessage;
            return DomainErrorType.NotFound;
        }

        if (!this.currentUserService.IsUserAuthorizedForResource(lineItem))
        {
            this.logger.LogWarning("User {UserId} attempted to access spendingPlan {LineItemName} {LineItemId} owned by {OwnerId}", this.currentUserService.UserId, this.LineItemName, lineItem.Id, lineItem.UserId);
            error = this.LineItemForbiddenMessage;
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