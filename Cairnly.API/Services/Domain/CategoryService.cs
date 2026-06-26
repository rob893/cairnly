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
using Cairnly.API.Models.Requests.Categories;
using Cairnly.API.Services.Auth;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for category management.
/// </summary>
public sealed class CategoryService : ICategoryService
{
    private readonly ILogger<CategoryService> logger;

    private readonly ICategoryRepository categoryRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public CategoryService(
        ILogger<CategoryService> logger,
        ICategoryRepository categoryRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<CategoryDto, int>> GetCategoriesAsync(CategoryQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.categoryRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList
            .Select(CategoryDto.FromEntity)
            .ToList();

        return new CursorPaginatedList<CategoryDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDto>> GetCategoryByIdAsync(int id, CancellationToken cancellationToken)
    {
        var category = await this.categoryRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (category == null)
        {
            this.logger.LogWarning("Category {CategoryId} not found", id);
            return Result<CategoryDto>.Failure(DomainErrorType.NotFound, "Category not found");
        }

        if (!this.CanAccess(category))
        {
            this.logger.LogWarning("User {UserId} attempted to access category {CategoryId} owned by {OwnerId}", this.currentUserService.UserId, id, category.UserId);
            return Result<CategoryDto>.Failure(DomainErrorType.Forbidden, "You can only access your own categories");
        }

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parentValidation = await this.ValidateParentAsync(request.ParentId, cancellationToken);

        if (!parentValidation.IsSuccess)
        {
            return Result<CategoryDto>.Failure(parentValidation.ErrorType!.Value, parentValidation.ErrorMessage!);
        }

        var category = new Category
        {
            UserId = this.currentUserService.UserId,
            Name = request.Name,
            Icon = request.Icon,
            Kind = request.Kind,
            ParentId = request.ParentId,
            IsSystem = false,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.categoryRepository.Add(category);
        await this.categoryRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created category {CategoryId} for user {UserId}", category.Id, category.UserId);

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await this.categoryRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (category == null)
        {
            this.logger.LogWarning("Category {CategoryId} not found for update", id);
            return Result<CategoryDto>.Failure(DomainErrorType.NotFound, "Category not found");
        }

        if (!this.CanModify(category))
        {
            this.logger.LogWarning("User {UserId} attempted to update category {CategoryId} (system={IsSystem}, owner={OwnerId})", this.currentUserService.UserId, id, category.IsSystem, category.UserId);
            return Result<CategoryDto>.Failure(DomainErrorType.Forbidden, "You can only update your own categories");
        }

        if (request.ParentId == id)
        {
            return Result<CategoryDto>.Failure(DomainErrorType.Validation, "A category cannot be its own parent");
        }

        var parentValidation = await this.ValidateParentAsync(request.ParentId, cancellationToken);

        if (!parentValidation.IsSuccess)
        {
            return Result<CategoryDto>.Failure(parentValidation.ErrorType!.Value, parentValidation.ErrorMessage!);
        }

        category.Name = request.Name;
        category.Icon = request.Icon;
        category.Kind = request.Kind;
        category.ParentId = request.ParentId;
        category.Metadata = request.Metadata ?? [];
        category.UpdatedById = this.currentUserService.UserId;

        await this.categoryRepository.SaveChangesAsync(cancellationToken);

        return Result<CategoryDto>.Success(CategoryDto.FromEntity(category));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteCategoryAsync(int id, CancellationToken cancellationToken)
    {
        var category = await this.categoryRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (category == null)
        {
            this.logger.LogWarning("Category {CategoryId} not found for deletion", id);
            return Result<bool>.Failure(DomainErrorType.NotFound, "Category not found");
        }

        if (!this.CanModify(category))
        {
            this.logger.LogWarning("User {UserId} attempted to delete category {CategoryId} (system={IsSystem}, owner={OwnerId})", this.currentUserService.UserId, id, category.IsSystem, category.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own categories");
        }

        this.categoryRepository.Remove(category);
        await this.categoryRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted category {CategoryId} for user {UserId}", id, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryDto>> PatchCategoryAsync(int id, JsonPatchDocument<UpdateCategoryRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<CategoryDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var category = await this.categoryRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (category == null)
        {
            return Result<CategoryDto>.Failure(DomainErrorType.NotFound, "Category not found");
        }

        if (!this.CanModify(category))
        {
            this.logger.LogWarning("User {UserId} attempted to patch category {CategoryId} (system={IsSystem}, owner={OwnerId})", this.currentUserService.UserId, id, category.IsSystem, category.UserId);
            return Result<CategoryDto>.Failure(DomainErrorType.Forbidden, "You can only update your own categories");
        }

        var request = UpdateCategoryRequest.FromEntity(category);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<CategoryDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<CategoryDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateCategoryAsync(id, request, cancellationToken);
    }

    private bool CanAccess(Category category)
    {
        return category.UserId == this.currentUserService.UserId
            || category.IsSystem
            || this.currentUserService.IsAdmin;
    }

    private bool CanModify(Category category)
    {
        if (this.currentUserService.IsAdmin)
        {
            return true;
        }

        return category.UserId == this.currentUserService.UserId && !category.IsSystem;
    }

    private async Task<Result<bool>> ValidateParentAsync(int? parentId, CancellationToken cancellationToken)
    {
        if (parentId == null)
        {
            return Result<bool>.Success(true);
        }

        var parent = await this.categoryRepository.GetByIdAsync(parentId.Value, track: false, cancellationToken);

        if (parent == null || !this.CanAccess(parent))
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "The specified parent category does not exist");
        }

        return Result<bool>.Success(true);
    }
}